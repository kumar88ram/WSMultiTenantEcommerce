using System.Linq;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class SubscriptionPlanService : ISubscriptionPlanService
{
    private readonly AdminDbContext _dbContext;

    public SubscriptionPlanService(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<SubscriptionPlanResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var plans = await _dbContext.SubscriptionPlans
            .OrderBy(p => p.Price)
            .ToListAsync(cancellationToken);

        return plans.Select(SubscriptionPlanResponse.FromEntity);
    }

    public async Task<SubscriptionPlanResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return plan is null ? null : SubscriptionPlanResponse.FromEntity(plan);
    }

    public async Task<SubscriptionPlanResponse> CreateAsync(SubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = new SubscriptionPlan
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Price = request.Price,
            Currency = request.Currency.ToUpperInvariant(),
            BillingPeriod = request.BillingPeriod,
            BillingMetadata = request.BillingMetadata.ToEntity()
        };

        _dbContext.SubscriptionPlans.Add(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return SubscriptionPlanResponse.FromEntity(plan);
    }

    public async Task<SubscriptionPlanResponse?> UpdateAsync(Guid id, SubscriptionPlanRequest request, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return null;
        }

        plan.Name = request.Name.Trim();
        plan.Description = request.Description;
        plan.Price = request.Price;
        plan.Currency = request.Currency.ToUpperInvariant();
        plan.BillingPeriod = request.BillingPeriod;
        plan.BillingMetadata = request.BillingMetadata.ToEntity();

        await _dbContext.SaveChangesAsync(cancellationToken);
        return SubscriptionPlanResponse.FromEntity(plan);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plan = await _dbContext.SubscriptionPlans.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plan is null)
        {
            return false;
        }

        var hasSubscriptions = await _dbContext.Subscriptions.AnyAsync(s => s.PlanId == id, cancellationToken);
        if (hasSubscriptions)
        {
            throw new InvalidOperationException("Cannot delete plan with active subscriptions.");
        }

        _dbContext.SubscriptionPlans.Remove(plan);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
