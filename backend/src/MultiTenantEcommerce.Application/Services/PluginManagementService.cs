using System.Linq;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class PluginManagementService : IPluginManagementService
{
    private readonly AdminDbContext _dbContext;

    public PluginManagementService(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<PluginDefinitionResponse>> GetDefinitionsAsync(CancellationToken cancellationToken = default)
    {
        var plugins = await _dbContext.Plugins
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return plugins.Select(PluginDefinitionResponse.FromEntity);
    }

    public async Task<PluginDefinitionResponse?> GetDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plugin = await _dbContext.Plugins.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        return plugin is null ? null : PluginDefinitionResponse.FromEntity(plugin);
    }

    public async Task<PluginDefinitionResponse> CreateDefinitionAsync(PluginDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var exists = await _dbContext.Plugins.AnyAsync(p => p.SystemKey == request.SystemKey, cancellationToken);
        if (exists)
        {
            throw new InvalidOperationException("Plugin with the same system key already exists.");
        }

        var plugin = new PluginDefinition
        {
            Name = request.Name.Trim(),
            SystemKey = request.SystemKey.Trim(),
            Version = request.Version,
            Description = request.Description,
            IsCore = request.IsCore,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Plugins.Add(plugin);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return PluginDefinitionResponse.FromEntity(plugin);
    }

    public async Task<PluginDefinitionResponse?> UpdateDefinitionAsync(Guid id, PluginDefinitionRequest request, CancellationToken cancellationToken = default)
    {
        var plugin = await _dbContext.Plugins.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plugin is null)
        {
            return null;
        }

        var conflict = await _dbContext.Plugins.AnyAsync(p => p.SystemKey == request.SystemKey && p.Id != id, cancellationToken);
        if (conflict)
        {
            throw new InvalidOperationException("Another plugin already uses the provided system key.");
        }

        plugin.Name = request.Name.Trim();
        plugin.SystemKey = request.SystemKey.Trim();
        plugin.Version = request.Version;
        plugin.Description = request.Description;
        plugin.IsCore = request.IsCore;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return PluginDefinitionResponse.FromEntity(plugin);
    }

    public async Task<bool> DeleteDefinitionAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var plugin = await _dbContext.Plugins.Include(p => p.TenantPlugins).FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (plugin is null)
        {
            return false;
        }

        if (plugin.IsCore)
        {
            throw new InvalidOperationException("Core plugins cannot be deleted.");
        }

        if (plugin.TenantPlugins.Any(tp => tp.IsEnabled))
        {
            throw new InvalidOperationException("Cannot delete a plugin that is enabled for tenants.");
        }

        _dbContext.Plugins.Remove(plugin);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<TenantPluginResponse> SetTenantPluginStateAsync(Guid tenantId, Guid pluginId, TenantPluginRequest request, CancellationToken cancellationToken = default)
    {
        var tenantExists = await _dbContext.Tenants.AnyAsync(t => t.Id == tenantId, cancellationToken);
        if (!tenantExists)
        {
            throw new InvalidOperationException("Tenant does not exist.");
        }

        var pluginExists = await _dbContext.Plugins.AnyAsync(p => p.Id == pluginId, cancellationToken);
        if (!pluginExists)
        {
            throw new InvalidOperationException("Plugin does not exist.");
        }

        var tenantPlugin = await _dbContext.TenantPlugins.FirstOrDefaultAsync(tp => tp.TenantId == tenantId && tp.PluginId == pluginId, cancellationToken);
        if (tenantPlugin is null)
        {
            tenantPlugin = new TenantPlugin
            {
                TenantId = tenantId,
                PluginId = pluginId
            };

            _dbContext.TenantPlugins.Add(tenantPlugin);
        }

        tenantPlugin.IsEnabled = request.IsEnabled;
        tenantPlugin.ConfigurationJson = request.ConfigurationJson;
        tenantPlugin.EnabledAt = request.IsEnabled ? DateTime.UtcNow : null;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return TenantPluginResponse.FromEntity(tenantPlugin);
    }

    public async Task<IEnumerable<TenantPluginResponse>> GetTenantPluginsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var plugins = await _dbContext.TenantPlugins
            .Where(tp => tp.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        return plugins.Select(TenantPluginResponse.FromEntity);
    }
}
