namespace MultiTenantEcommerce.Application.Abstractions;

public interface IThemeCloneService
{
    Task<Guid?> CloneTenantThemeAsync(Guid sourceTenantId, Guid targetTenantId, Guid adminId, CancellationToken cancellationToken = default);
}
