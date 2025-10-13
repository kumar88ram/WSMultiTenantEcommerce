using System.Linq;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class AdminTenantService : IAdminTenantService
{
    private readonly AdminDbContext _dbContext;

    public AdminTenantService(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<TenantResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var tenants = await _dbContext.Tenants
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

        return tenants.Select(TenantResponse.FromEntity);
    }

    public async Task<TenantResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        return tenant is null ? null : TenantResponse.FromEntity(tenant);
    }

    public async Task<TenantResponse?> UpdateAsync(Guid id, UpdateTenantRequest request, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            tenant.Name = request.Name.Trim();
        }

        if (request.CustomDomain is not null)
        {
            tenant.CustomDomain = string.IsNullOrWhiteSpace(request.CustomDomain)
                ? null
                : request.CustomDomain.Trim().ToLowerInvariant();
        }

        if (request.PlanId.HasValue && request.PlanId.Value != Guid.Empty)
        {
            tenant.PlanId = request.PlanId.Value;
        }

        if (request.IsActive.HasValue)
        {
            tenant.IsActive = request.IsActive.Value;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return TenantResponse.FromEntity(tenant);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var tenant = await _dbContext.Tenants.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (tenant is null)
        {
            return false;
        }

        _dbContext.Tenants.Remove(tenant);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
