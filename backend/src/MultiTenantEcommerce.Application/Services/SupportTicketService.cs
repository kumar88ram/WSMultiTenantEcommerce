using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Support;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class SupportTicketService : ISupportTicketService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<SupportTicketService> _logger;

    public SupportTicketService(
        ApplicationDbContext dbContext,
        ITenantResolver tenantResolver,
        ILogger<SupportTicketService> logger)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    public async Task<SupportTicketDto> CreateAsync(CreateSupportTicketRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Subject))
        {
            throw new ArgumentException("Subject is required", nameof(request.Subject));
        }

        if (string.IsNullOrWhiteSpace(request.Message))
        {
            throw new ArgumentException("Message is required", nameof(request.Message));
        }

        var tenantId = EnsureTenantContext();
        var ticket = new SupportTicket
        {
            TenantId = tenantId,
            Subject = request.Subject.Trim(),
            Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
            Status = SupportTicketStatus.Open,
            CreatedByUserId = request.AuthorType == SupportTicketActorType.Customer ? null : request.CreatedByUserId,
            CustomerName = string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim(),
            CustomerEmail = string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim().ToLowerInvariant()
        };

        var message = new SupportTicketMessage
        {
            TenantId = tenantId,
            SupportTicket = ticket,
            Body = request.Message.Trim(),
            AuthorType = request.AuthorType,
            AuthorUserId = request.AuthorType == SupportTicketActorType.Customer ? null : request.CreatedByUserId,
            AuthorName = request.AuthorType == SupportTicketActorType.Customer
                ? (string.IsNullOrWhiteSpace(request.CustomerName) ? null : request.CustomerName.Trim())
                : null,
            AuthorEmail = request.AuthorType == SupportTicketActorType.Customer
                ? (string.IsNullOrWhiteSpace(request.CustomerEmail) ? null : request.CustomerEmail.Trim().ToLowerInvariant())
                : null
        };

        if (request.Attachments is not null)
        {
            foreach (var attachment in request.Attachments.Where(a => !string.IsNullOrWhiteSpace(a.FileName) && !string.IsNullOrWhiteSpace(a.Url)))
            {
                message.Attachments.Add(new SupportTicketAttachment
                {
                    TenantId = tenantId,
                    FileName = attachment.FileName.Trim(),
                    Url = attachment.Url.Trim()
                });
            }
        }

        ticket.Messages.Add(message);

        await _dbContext.SupportTickets.AddAsync(ticket, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Support ticket {TicketId} created for tenant {TenantId}", ticket.Id, tenantId);

        await _dbContext.Entry(ticket).Reference(t => t.AssignedToUser).LoadAsync(cancellationToken);
        await _dbContext.Entry(ticket).Collection(t => t.Messages)
            .Query()
            .Include(m => m.Attachments)
            .Include(m => m.AuthorUser)
            .LoadAsync(cancellationToken);

        return MapTicket(ticket, null);
    }

    public async Task<SupportTicketDto?> GetByIdAsync(Guid ticketId, CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenantContext();
        var ticket = await LoadTicketAsync(ticketId, tenantId, includeMessages: true, cancellationToken);
        return ticket is null ? null : MapTicket(ticket, null);
    }

    public async Task<SupportTicketListResult> GetAsync(SupportTicketListQuery query, CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenantContext();
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();

        var ticketsQuery = _dbContext.SupportTickets
            .AsNoTracking()
            .Where(t => t.TenantId == tenantId);

        if (query.Status.HasValue)
        {
            ticketsQuery = ticketsQuery.Where(t => t.Status == query.Status.Value);
        }

        if (query.AssignedToUserId.HasValue)
        {
            ticketsQuery = ticketsQuery.Where(t => t.AssignedToUserId == query.AssignedToUserId.Value);
        }

        if (!string.IsNullOrEmpty(search))
        {
            ticketsQuery = ticketsQuery.Where(t =>
                EF.Functions.Like(t.Subject, $"%{search}%") ||
                (t.CustomerEmail != null && EF.Functions.Like(t.CustomerEmail, $"%{search}%")) ||
                (t.CustomerName != null && EF.Functions.Like(t.CustomerName, $"%{search}%")));
        }

        var totalCount = await ticketsQuery.CountAsync(cancellationToken);

        var items = await ticketsQuery
            .OrderByDescending(t => t.UpdatedAt ?? t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        var summaries = items.Select(i => new SupportTicketSummaryDto(
            i.Id,
            i.TenantId,
            null,
            i.Subject,
            i.Status,
            i.CreatedAt,
            i.UpdatedAt,
            i.AssignedTo,
            i.CustomerName,
            i.LastMessageAt)).ToList();

        return new SupportTicketListResult(page, pageSize, totalCount, summaries);
    }

    public async Task<SupportTicketDto?> UpdateAsync(Guid ticketId, UpdateSupportTicketRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = EnsureTenantContext();
        var ticket = await LoadTicketAsync(ticketId, tenantId, includeMessages: true, cancellationToken);
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
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapTicket(ticket, null);
    }

    public async Task<SupportTicketDto?> AddMessageAsync(Guid ticketId, AddSupportTicketMessageRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Body))
        {
            throw new ArgumentException("Message body is required", nameof(request.Body));
        }

        var tenantId = EnsureTenantContext();
        var ticket = await LoadTicketAsync(ticketId, tenantId, includeMessages: true, cancellationToken);
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
            TenantId = tenantId,
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
                    TenantId = tenantId,
                    FileName = attachment.FileName.Trim(),
                    Url = attachment.Url.Trim()
                });
            }
        }

        await _dbContext.SupportTicketMessages.AddAsync(message, cancellationToken);
        ticket.Messages.Add(message);
        ticket.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        await _dbContext.Entry(message).Collection(m => m.Attachments).LoadAsync(cancellationToken);
        if (message.AuthorUserId.HasValue)
        {
            await _dbContext.Entry(message).Reference(m => m.AuthorUser).LoadAsync(cancellationToken);
        }

        return MapTicket(ticket, null);
    }

    public async Task<bool> ValidateCustomerAccessAsync(CustomerTicketLookup lookup, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(lookup.CustomerEmail))
        {
            return false;
        }

        var tenantId = EnsureTenantContext();
        var email = lookup.CustomerEmail.Trim().ToLowerInvariant();
        return await _dbContext.SupportTickets.AsNoTracking()
            .AnyAsync(t => t.Id == lookup.TicketId && t.TenantId == tenantId && t.CustomerEmail != null && t.CustomerEmail == email, cancellationToken);
    }

    private Guid EnsureTenantContext()
    {
        if (_tenantResolver.CurrentTenantId == Guid.Empty)
        {
            throw new InvalidOperationException("Tenant context is not resolved for support ticket operations.");
        }

        return _tenantResolver.CurrentTenantId;
    }

    private async Task<SupportTicket?> LoadTicketAsync(Guid ticketId, Guid tenantId, bool includeMessages, CancellationToken cancellationToken)
    {
        IQueryable<SupportTicket> query = _dbContext.SupportTickets
            .Where(t => t.Id == ticketId && t.TenantId == tenantId)
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

    internal static SupportTicketDto MapTicket(SupportTicket ticket, string? tenantName)
    {
        var messages = ticket.Messages
            .OrderBy(m => m.CreatedAt)
            .Select(MapMessage)
            .ToList();

        return new SupportTicketDto(
            ticket.Id,
            ticket.TenantId,
            tenantName,
            ticket.Subject,
            ticket.Description,
            ticket.Status,
            ticket.AssignedToUserId,
            ticket.AssignedToUser?.UserName,
            ticket.CreatedByUserId,
            ticket.CustomerName,
            ticket.CustomerEmail,
            ticket.CreatedAt,
            ticket.UpdatedAt,
            messages);
    }

    internal static SupportTicketMessageDto MapMessage(SupportTicketMessage message)
    {
        var attachments = message.Attachments
            .OrderBy(a => a.CreatedAt)
            .Select(a => new SupportTicketAttachmentDto(a.Id, a.FileName, a.Url))
            .ToList();

        return new SupportTicketMessageDto(
            message.Id,
            message.Body,
            message.AuthorType,
            message.AuthorUserId,
            message.AuthorUser?.UserName ?? message.AuthorName,
            message.AuthorEmail,
            message.CreatedAt,
            attachments);
    }
}
