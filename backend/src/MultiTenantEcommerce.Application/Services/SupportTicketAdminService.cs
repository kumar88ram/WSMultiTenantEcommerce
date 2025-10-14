using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Support;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.MultiTenancy;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class SupportTicketAdminService : ISupportTicketAdminService
{
    private readonly AdminDbContext _adminDbContext;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly ILogger<SupportTicketAdminService> _logger;

    public SupportTicketAdminService(
        AdminDbContext adminDbContext,
        ITenantDbContextFactory tenantDbContextFactory,
        ILogger<SupportTicketAdminService> logger)
    {
        _adminDbContext = adminDbContext;
        _tenantDbContextFactory = tenantDbContextFactory;
        _logger = logger;
    }

    public async Task<SupportTicketListResult> GetAsync(Guid? tenantId, SupportTicketListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var tenantInfos = await ResolveTenantsAsync(tenantId, cancellationToken);

        var aggregates = new List<(SupportTicketSummaryDto Summary, DateTime SortKey)>();
        var totalCount = 0;
        foreach (var tenantInfo in tenantInfos)
        {
            await using var tenantDb = _tenantDbContextFactory.CreateDbContext(tenantInfo.ConnectionString, tenantInfo.Id, tenantInfo.Identifier);
            var ticketsQuery = ApplyFilters(tenantDb.SupportTickets.AsNoTracking(), query);

            var count = await ticketsQuery.CountAsync(cancellationToken);
            totalCount += count;

            var items = await ticketsQuery
                .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
                .Select(t => new
                {
                    t.Id,
                    t.TenantId,
                    t.Subject,
                    t.Status,
                    t.CreatedAt,
                    t.UpdatedAt,
                    AssignedTo = t.AssignedToUser != null ? t.AssignedToUser.UserName : null,
                    t.CustomerName,
                    LastMessageAt = t.Messages.OrderByDescending(m => m.CreatedAt).Select(m => (DateTime?)m.CreatedAt).FirstOrDefault()
                })
                .ToListAsync(cancellationToken);

            aggregates.AddRange(items.Select(item =>
            {
                var sortKey = item.LastMessageAt ?? item.UpdatedAt ?? item.CreatedAt;
                var summary = new SupportTicketSummaryDto(
                    item.Id,
                    item.TenantId,
                    tenantInfo.Name,
                    item.Subject,
                    item.Status,
                    item.CreatedAt,
                    item.UpdatedAt,
                    item.AssignedTo,
                    item.CustomerName,
                    item.LastMessageAt);
                return (summary, sortKey);
            }));
        }

        var ordered = aggregates
            .OrderByDescending(x => x.SortKey)
            .ThenBy(x => x.Summary.Id)
            .ToList();

        var paged = ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => x.Summary)
            .ToList();

        return new SupportTicketListResult(page, pageSize, totalCount, paged);
    }

    public async Task<SupportTicketDto?> GetByIdAsync(Guid tenantId, Guid ticketId, CancellationToken cancellationToken = default)
    {
        var tenantInfo = await ResolveTenantAsync(tenantId, cancellationToken);
        if (tenantInfo is null)
        {
            return null;
        }

        await using var tenantDb = _tenantDbContextFactory.CreateDbContext(tenantInfo.ConnectionString, tenantInfo.Id, tenantInfo.Identifier);
        var ticket = await LoadTicketAsync(tenantDb, ticketId, includeMessages: true, cancellationToken);
        return ticket is null ? null : SupportTicketService.MapTicket(ticket, tenantInfo.Name);
    }

    public async Task<SupportTicketDto?> UpdateAsync(Guid tenantId, Guid ticketId, UpdateSupportTicketRequest request, CancellationToken cancellationToken = default)
    {
        var tenantInfo = await ResolveTenantAsync(tenantId, cancellationToken);
        if (tenantInfo is null)
        {
            return null;
        }

        await using var tenantDb = _tenantDbContextFactory.CreateDbContext(tenantInfo.ConnectionString, tenantInfo.Id, tenantInfo.Identifier);
        var ticket = await LoadTicketAsync(tenantDb, ticketId, includeMessages: true, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Subject))
        {
            ticket.Subject = request.Subject.Trim();
        }

        if (request.Description is not null)
        {
            ticket.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        }

        if (request.Status.HasValue)
        {
            ticket.Status = request.Status.Value;
        }

        if (request.AssignedToUserId.HasValue)
        {
            ticket.AssignedToUserId = request.AssignedToUserId == Guid.Empty ? null : request.AssignedToUserId;
        }

        ticket.UpdatedAt = DateTime.UtcNow;
        await tenantDb.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Support ticket {TicketId} updated by super admin for tenant {TenantId}", ticket.Id, tenantInfo.Id);

        return SupportTicketService.MapTicket(ticket, tenantInfo.Name);
    }

    public async Task<SupportTicketDto?> AddMessageAsync(Guid tenantId, Guid ticketId, AddSupportTicketMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ArgumentException("Message body is required", nameof(request.Body));
        }

        var tenantInfo = await ResolveTenantAsync(tenantId, cancellationToken);
        if (tenantInfo is null)
        {
            return null;
        }

        await using var tenantDb = _tenantDbContextFactory.CreateDbContext(tenantInfo.ConnectionString, tenantInfo.Id, tenantInfo.Identifier);
        var ticket = await LoadTicketAsync(tenantDb, ticketId, includeMessages: true, cancellationToken);
        if (ticket is null)
        {
            return null;
        }

        if (request.AuthorType == SupportTicketActorType.Customer)
        {
            if (string.IsNullOrWhiteSpace(request.AuthorEmail) ||
                !string.Equals(ticket.CustomerEmail, request.AuthorEmail.Trim(), StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Customer verification failed for ticket message.");
            }
        }

        var message = new SupportTicketMessage
        {
            TenantId = tenantInfo.Id,
            SupportTicketId = ticket.Id,
            Body = request.Body.Trim(),
            AuthorType = request.AuthorType,
            AuthorUserId = request.AuthorType == SupportTicketActorType.Customer ? null : request.AuthorUserId,
            AuthorName = string.IsNullOrWhiteSpace(request.AuthorName) ? null : request.AuthorName.Trim(),
            AuthorEmail = string.IsNullOrWhiteSpace(request.AuthorEmail) ? null : request.AuthorEmail.Trim().ToLowerInvariant()
        };

        if (request.Attachments is not null)
        {
            foreach (var attachment in request.Attachments.Where(a => !string.IsNullOrWhiteSpace(a.FileName) && !string.IsNullOrWhiteSpace(a.Url)))
            {
                message.Attachments.Add(new SupportTicketAttachment
                {
                    TenantId = tenantInfo.Id,
                    FileName = attachment.FileName.Trim(),
                    Url = attachment.Url.Trim()
                });
            }
        }

        await tenantDb.SupportTicketMessages.AddAsync(message, cancellationToken);
        ticket.Messages.Add(message);
        ticket.UpdatedAt = DateTime.UtcNow;
        await tenantDb.SaveChangesAsync(cancellationToken);
        await tenantDb.Entry(message).Collection(m => m.Attachments).LoadAsync(cancellationToken);
        if (message.AuthorUserId.HasValue)
        {
            await tenantDb.Entry(message).Reference(m => m.AuthorUser).LoadAsync(cancellationToken);
        }

        _logger.LogInformation("Support ticket {TicketId} message added by super admin for tenant {TenantId}", ticket.Id, tenantInfo.Id);

        return SupportTicketService.MapTicket(ticket, tenantInfo.Name);
    }

    private IQueryable<SupportTicket> ApplyFilters(IQueryable<SupportTicket> query, SupportTicketListQuery filters)
    {
        if (filters.Status.HasValue)
        {
            query = query.Where(t => t.Status == filters.Status.Value);
        }

        if (filters.AssignedToUserId.HasValue)
        {
            query = query.Where(t => t.AssignedToUserId == filters.AssignedToUserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(filters.Search))
        {
            var search = filters.Search.Trim();
            query = query.Where(t =>
                EF.Functions.Like(t.Subject, $"%{search}%") ||
                (t.CustomerEmail != null && EF.Functions.Like(t.CustomerEmail, $"%{search}%")) ||
                (t.CustomerName != null && EF.Functions.Like(t.CustomerName, $"%{search}%")));
        }

        return query;
    }

    private async Task<List<TenantInfo>> ResolveTenantsAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        IQueryable<TenantInfo> query = _adminDbContext.Tenants
            .AsNoTracking()
            .Select(t => new TenantInfo(t.Id, t.Name, t.Subdomain, t.DbConnectionString));

        if (tenantId.HasValue)
        {
            query = query.Where(t => t.Id == tenantId.Value);
        }

        var tenants = await query.ToListAsync(cancellationToken);
        return tenants;
    }

    private async Task<TenantInfo?> ResolveTenantAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        return await _adminDbContext.Tenants
            .AsNoTracking()
            .Where(t => t.Id == tenantId)
            .Select(t => new TenantInfo(t.Id, t.Name, t.Subdomain, t.DbConnectionString))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static async Task<SupportTicket?> LoadTicketAsync(ApplicationDbContext tenantDbContext, Guid ticketId, bool includeMessages, CancellationToken cancellationToken)
    {
        IQueryable<SupportTicket> query = tenantDbContext.SupportTickets
            .Where(t => t.Id == ticketId)
            .Include(t => t.AssignedToUser)
            .Include(t => t.CreatedByUser);

        if (includeMessages)
        {
            query = query
                .Include(t => t.Messages)
                .ThenInclude(m => m.Attachments)
                .Include(t => t.Messages)
                .ThenInclude(m => m.AuthorUser);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record TenantInfo(Guid Id, string Name, string Identifier, string ConnectionString);
}
