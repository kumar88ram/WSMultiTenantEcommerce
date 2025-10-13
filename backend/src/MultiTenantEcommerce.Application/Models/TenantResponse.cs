using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models;

public record TenantResponse(
    Guid Id,
    string Name,
    string Subdomain,
    string? CustomDomain,
    string PlanId,
    bool IsActive,
    DateTime CreatedAt
)
{
    public static TenantResponse FromEntity(Tenant tenant) => new(
        tenant.Id,
        tenant.Name,
        tenant.Subdomain,
        tenant.CustomDomain,
        tenant.PlanId,
        tenant.IsActive,
        tenant.CreatedAt);
}
