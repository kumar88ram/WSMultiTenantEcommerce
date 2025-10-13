using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Presentation.MultiTenancy;

public class MultiTenantContext : ITenantResolver
{
    public Guid CurrentTenantId { get; set; }
    public string? TenantIdentifier { get; set; }
}
