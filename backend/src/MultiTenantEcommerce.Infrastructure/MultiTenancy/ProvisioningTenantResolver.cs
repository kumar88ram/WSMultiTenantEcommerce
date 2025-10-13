using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Infrastructure.MultiTenancy;

internal sealed class ProvisioningTenantResolver : ITenantResolver
{
    public ProvisioningTenantResolver(Guid tenantId, string? identifier = null, string? connectionString = null)
    {
        CurrentTenantId = tenantId;
        TenantIdentifier = identifier;
        ConnectionString = connectionString;
    }

    public Guid CurrentTenantId { get; }
    public string? TenantIdentifier { get; }
    public string? ConnectionString { get; }
}
