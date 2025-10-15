using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Tenant;

[ApiController]
[Route("tenant/theme")]
[Authorize(Roles = "TenantAdmin")]
public class TenantThemeController : ControllerBase
{
    private readonly ITenantResolver _tenantResolver;
    private readonly IThemeService _themeService;

    public TenantThemeController(ITenantResolver tenantResolver, IThemeService themeService)
    {
        _tenantResolver = tenantResolver;
        _themeService = themeService;
    }

    [HttpGet]
    public async Task<ActionResult<TenantThemeDto>> GetActiveTheme(CancellationToken cancellationToken)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        if (tenantId == Guid.Empty)
        {
            return NotFound("Tenant context could not be resolved.");
        }

        var tenantTheme = await _themeService.GetActiveThemeAsync(tenantId, cancellationToken);
        if (tenantTheme is null)
        {
            return NotFound();
        }

        return Ok(tenantTheme.ToTenantThemeDto());
    }

    [HttpPatch("variables")]
    public async Task<ActionResult<IEnumerable<ThemeVariableDto>>> UpdateVariables([FromBody] ThemeVariablesRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        if (tenantId == Guid.Empty)
        {
            return NotFound("Tenant context could not be resolved.");
        }

        var tenantTheme = await _themeService.GetActiveThemeAsync(tenantId, cancellationToken);
        if (tenantTheme is null)
        {
            return NotFound("No active theme is set for this tenant.");
        }

        var variables = request.Variables ?? new Dictionary<string, string>();
        var updated = await _themeService.UpdateVariablesAsync(tenantTheme.Id, variables, cancellationToken);
        return Ok(updated.Select(v => new ThemeVariableDto(v.Key, v.Value)));
    }

    [HttpDelete("variables")]
    public async Task<IActionResult> ResetVariables(CancellationToken cancellationToken)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        if (tenantId == Guid.Empty)
        {
            return NotFound("Tenant context could not be resolved.");
        }

        var tenantTheme = await _themeService.GetActiveThemeAsync(tenantId, cancellationToken);
        if (tenantTheme is null)
        {
            return NotFound();
        }

        await _themeService.UpdateVariablesAsync(tenantTheme.Id, new Dictionary<string, string>(), cancellationToken);
        return NoContent();
    }

    public record ThemeVariablesRequest(IDictionary<string, string>? Variables);
}
