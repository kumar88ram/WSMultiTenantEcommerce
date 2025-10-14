namespace MultiTenantEcommerce.Application.Models.Plugins;

public record PluginActivationState(
    Guid TenantPluginId,
    Guid TenantId,
    Guid PluginId,
    string Name,
    string SystemKey,
    bool IsEnabled,
    DateTime? EnabledAt,
    string? ConfigurationJson);
