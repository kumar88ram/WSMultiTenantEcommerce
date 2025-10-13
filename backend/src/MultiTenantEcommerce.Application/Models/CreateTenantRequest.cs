namespace MultiTenantEcommerce.Application.Models;

public record CreateTenantRequest(
    string Name,
    string Subdomain,
    string? CustomDomain,
    Guid PlanId
);
