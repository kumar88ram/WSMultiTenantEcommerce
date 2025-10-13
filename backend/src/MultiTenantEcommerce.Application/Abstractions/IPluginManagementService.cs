using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPluginManagementService
{
    Task<IEnumerable<PluginDefinitionResponse>> GetDefinitionsAsync(CancellationToken cancellationToken = default);
    Task<PluginDefinitionResponse?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<PluginDefinitionResponse> CreateDefinitionAsync(PluginDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<PluginDefinitionResponse?> UpdateDefinitionAsync(Guid id, PluginDefinitionRequest request, CancellationToken cancellationToken = default);
    Task<bool> DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default);
    Task<TenantPluginResponse> SetTenantPluginStateAsync(Guid tenantId, Guid pluginId, TenantPluginRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantPluginResponse>> GetTenantPluginsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
