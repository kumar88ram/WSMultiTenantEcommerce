using System.Linq;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly AdminDbContext _dbContext;

    public SubscriptionService(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SubscriptionResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var subscriptions = await _dbContext.Subscriptions
            .OrderByDescending(s => s.StartDate)
            .ToListAsync(cancellationToken);

        return subscriptions.Select(SubscriptionResponse.FromEntity);
    }

    public async Task<SubscriptionResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        return subscription is null ? null : SubscriptionResponse.FromEntity(subscription);
    }

    public async Task<SubscriptionResponse> CreateAsync(SubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        await EnsureReferencesAsync(request, cancellationToken);

        var subscription = new Subscription
        {
            TenantId = request.TenantId,
            PlanId = request.PlanId,
            Status = request.Status,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            NextBillingDate = request.NextBillingDate,
            Amount = request.Amount,
            Currency = request.Currency.ToUpperInvariant(),
            BillingReference = request.BillingReference
        };

        _dbContext.Subscriptions.Add(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubscriptionResponse.FromEntity(subscription);
    }

    public async Task<SubscriptionResponse?> UpdateAsync(Guid id, SubscriptionRequest request, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subscription is null)
        {
            return null;
        }

        await EnsureReferencesAsync(request, cancellationToken);

        subscription.TenantId = request.TenantId;
        subscription.PlanId = request.PlanId;
        subscription.Status = request.Status;
        subscription.StartDate = request.StartDate;
        subscription.EndDate = request.EndDate;
        subscription.NextBillingDate = request.NextBillingDate;
        subscription.Amount = request.Amount;
        subscription.Currency = request.Currency.ToUpperInvariant();
        subscription.BillingReference = request.BillingReference;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return SubscriptionResponse.FromEntity(subscription);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var subscription = await _dbContext.Subscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken);
        if (subscription is null)
        {
            return false;
        }

        _dbContext.Subscriptions.Remove(subscription);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task EnsureReferencesAsync(SubscriptionRequest request, CancellationToken cancellationToken)
    {
        var tenantExists = await _dbContext.Tenants.AnyAsync(t => t.Id == request.TenantId, cancellationToken);
        if (!tenantExists)
        {
            throw new InvalidOperationException("Tenant does not exist.");
        }

        var planExists = await _dbContext.SubscriptionPlans.AnyAsync(p => p.Id == request.PlanId, cancellationToken);
        if (!planExists)
        {
            throw new InvalidOperationException("Subscription plan does not exist.");
        }
    }
}
