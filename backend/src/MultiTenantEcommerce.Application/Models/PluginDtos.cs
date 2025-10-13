using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models;

public record PluginDefinitionRequest(
    string Name,
    string SystemKey,
    string Version,
    string Description,
    bool IsCore
);

public record PluginDefinitionResponse(
    Guid Id,
    string Name,
    string SystemKey,
    string Version,
    string Description,
    bool IsCore,
    DateTime CreatedAt
)
{
    public static PluginDefinitionResponse FromEntity(PluginDefinition plugin) => new(
        plugin.Id,
        plugin.Name,
        plugin.SystemKey,
        plugin.Version,
        plugin.Description,
        plugin.IsCore,
        plugin.CreatedAt
    );
}

public record TenantPluginRequest(bool IsEnabled, string? ConfigurationJson);

public record TenantPluginResponse(
    Guid Id,
    Guid TenantId,
    Guid PluginId,
    bool IsEnabled,
    DateTime? EnabledAt,
    string? ConfigurationJson
)
{
    public static TenantPluginResponse FromEntity(TenantPlugin tenantPlugin) => new(
        tenantPlugin.Id,
        tenantPlugin.TenantId,
        tenantPlugin.PluginId,
        tenantPlugin.IsEnabled,
        tenantPlugin.EnabledAt,
        tenantPlugin.ConfigurationJson
    );
}
