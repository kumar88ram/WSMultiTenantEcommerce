using MultiTenantEcommerce.Application.Models.Plugins;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPluginManagerService
{
    Task<IReadOnlyList<PluginActivationState>> GetTenantPluginsAsync(Guid tenantId, CancellationToken cancellationToken = default);

    Task<PluginActivationState?> GetTenantPluginAsync(Guid tenantId, string systemKey, CancellationToken cancellationToken = default);

    Task<bool> IsPluginEnabledAsync(Guid tenantId, string systemKey, CancellationToken cancellationToken = default);

    Task EnsurePluginEnabledAsync(Guid tenantId, string systemKey, CancellationToken cancellationToken = default);
}
