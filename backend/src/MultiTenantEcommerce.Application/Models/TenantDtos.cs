namespace MultiTenantEcommerce.Application.Models;

public record UpdateTenantRequest(
    string? Name,
    string? CustomDomain,
    Guid? PlanId,
    bool? IsActive
);
