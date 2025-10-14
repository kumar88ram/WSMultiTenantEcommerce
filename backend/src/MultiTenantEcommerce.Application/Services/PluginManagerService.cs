using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Plugins;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class PluginManagerService : IPluginManagerService
{
    private readonly AdminDbContext _adminDbContext;

    public PluginManagerService(AdminDbContext adminDbContext)
    {
        _adminDbContext = adminDbContext;
    }

    public async Task<IReadOnlyList<PluginActivationState>> GetTenantPluginsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var plugins = await _adminDbContext.TenantPlugins
            .Include(tp => tp.Plugin)
            .Where(tp => tp.TenantId == tenantId)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return plugins
            .Select(tp => new PluginActivationState(
                tp.Id,
                tp.TenantId,
                tp.PluginId,
                tp.Plugin.Name,
                tp.Plugin.SystemKey,
                tp.IsEnabled,
                tp.EnabledAt,
                tp.ConfigurationJson))
            .ToList();
    }

    public async Task<PluginActivationState?> GetTenantPluginAsync(Guid tenantId, string systemKey, CancellationToken cancellationToken = default)
    {
        var plugin = await _adminDbContext.TenantPlugins
            .Include(tp => tp.Plugin)
            .Where(tp => tp.TenantId == tenantId && tp.Plugin.SystemKey == systemKey)
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (plugin is null)
        {
            return null;
        }

        return new PluginActivationState(
            plugin.Id,
            plugin.TenantId,
            plugin.PluginId,
            plugin.Plugin.Name,
            plugin.Plugin.SystemKey,
            plugin.IsEnabled,
            plugin.EnabledAt,
            plugin.ConfigurationJson);
    }

    public async Task<bool> IsPluginEnabledAsync(Guid tenantId, string systemKey, CancellationToken cancellationToken = default)
    {
        return await _adminDbContext.TenantPlugins
            .Include(tp => tp.Plugin)
            .Where(tp => tp.TenantId == tenantId && tp.Plugin.SystemKey == systemKey)
            .Select(tp => tp.IsEnabled)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task EnsurePluginEnabledAsync(Guid tenantId, string systemKey, CancellationToken cancellationToken = default)
    {
        var pluginState = await GetTenantPluginAsync(tenantId, systemKey, cancellationToken);
        if (pluginState is null || !pluginState.IsEnabled)
        {
            throw new InvalidOperationException($"Plugin '{systemKey}' is not enabled for the current tenant.");
        }
    }
}
