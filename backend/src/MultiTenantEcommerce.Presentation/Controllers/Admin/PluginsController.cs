using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/plugins")]
[Authorize(Roles = "SuperAdmin")]
public class PluginsController : ControllerBase
{
    private readonly IPluginManagementService _pluginService;

    public PluginsController(IPluginManagementService pluginService)
    {
        _pluginService = pluginService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<PluginDefinitionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlugins(CancellationToken cancellationToken)
    {
        var plugins = await _pluginService.GetDefinitionsAsync(cancellationToken);
        return Ok(plugins);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(PluginDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlugin(Guid id, CancellationToken cancellationToken)
    {
        var plugin = await _pluginService.GetDefinitionAsync(id, cancellationToken);
        return plugin is null ? NotFound() : Ok(plugin);
    }

    [HttpPost]
    [ProducesResponseType(typeof(PluginDefinitionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreatePlugin([FromBody] PluginDefinitionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var plugin = await _pluginService.CreateDefinitionAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetPlugin), new { id = plugin.Id }, plugin);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(PluginDefinitionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlugin(Guid id, [FromBody] PluginDefinitionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var plugin = await _pluginService.UpdateDefinitionAsync(id, request, cancellationToken);
            return plugin is null ? NotFound() : Ok(plugin);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlugin(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _pluginService.DeleteDefinitionAsync(id, cancellationToken);
            return deleted ? NoContent() : NotFound();
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpGet("tenants/{tenantId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<TenantPluginResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenantPlugins(Guid tenantId, CancellationToken cancellationToken)
    {
        var plugins = await _pluginService.GetTenantPluginsAsync(tenantId, cancellationToken);
        return Ok(plugins);
    }

    [HttpPut("{pluginId:guid}/tenants/{tenantId:guid}")]
    [ProducesResponseType(typeof(TenantPluginResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> SetTenantPlugin(Guid pluginId, Guid tenantId, [FromBody] TenantPluginRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var response = await _pluginService.SetTenantPluginStateAsync(tenantId, pluginId, request, cancellationToken);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }
}
