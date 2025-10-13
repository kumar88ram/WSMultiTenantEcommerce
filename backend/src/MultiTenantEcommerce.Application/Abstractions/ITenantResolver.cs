namespace MultiTenantEcommerce.Application.Abstractions;

public interface ITenantResolver
{
    Guid CurrentTenantId { get; }
    string? TenantIdentifier { get; }
}
