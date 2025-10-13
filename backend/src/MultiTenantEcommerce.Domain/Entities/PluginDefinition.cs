namespace MultiTenantEcommerce.Domain.Entities;

public class PluginDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string SystemKey { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public string Description { get; set; } = string.Empty;
    public bool IsCore { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<TenantPlugin> TenantPlugins { get; set; } = new List<TenantPlugin>();
}

public class TenantPlugin
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid PluginId { get; set; }
    public PluginDefinition Plugin { get; set; } = null!;
    public bool IsEnabled { get; set; }
    public DateTime? EnabledAt { get; set; }
    public string? ConfigurationJson { get; set; }
}
