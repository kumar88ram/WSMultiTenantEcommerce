using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Application.Services;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/themes")]
[Authorize(Roles = "SuperAdmin")]
public class ThemesController : ControllerBase
{
    private readonly IThemeService _themeService;
    private readonly ThemeBuilderService _themeBuilderService;
    private readonly IWebHostEnvironment _environment;

    public ThemesController(IThemeService themeService, ThemeBuilderService themeBuilderService, IWebHostEnvironment environment)
    {
        _themeService = themeService;
        _themeBuilderService = themeBuilderService;
        _environment = environment;
    }

    [HttpPost("upload")]
    [RequestSizeLimit(200_000_000)]
    public async Task<ActionResult<ThemeSummaryDto>> UploadTheme([FromForm] ThemeUploadRequest request, CancellationToken cancellationToken)
    {
        if (request.Package is null || request.Package.Length == 0)
        {
            return BadRequest("A theme package (.zip) is required.");
        }

        await using var packageStream = new MemoryStream();
        await request.Package.CopyToAsync(packageStream, cancellationToken);
        packageStream.Position = 0;

        var manifest = await _themeBuilderService.ParseManifestAsync(packageStream, cancellationToken);
        packageStream.Position = 0;

        var uploadContext = new ThemeUploadContext(
            request.Package.FileName,
            packageStream,
            _environment.ContentRootPath,
            manifest);

        var theme = await _themeService.UploadThemeAsync(uploadContext, cancellationToken);
        return CreatedAtAction(nameof(GetThemes), new { theme.Id }, theme.ToSummaryDto());
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ThemeSummaryDto>>> GetThemes(CancellationToken cancellationToken)
    {
        var themes = await _themeService.GetThemesAsync(cancellationToken);
        return Ok(themes.Select(t => t.ToSummaryDto()));
    }

    [HttpPost("{id:guid}/activate/{tenantId:guid}")]
    public async Task<ActionResult<TenantThemeDto>> ActivateTheme(Guid id, Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantTheme = await _themeService.ActivateThemeAsync(id, tenantId, cancellationToken);
        if (tenantTheme is null)
        {
            return NotFound();
        }

        var hydratedTheme = await _themeService.GetActiveThemeAsync(tenantId, cancellationToken)
                             ?? tenantTheme;

        return Ok(hydratedTheme.ToTenantThemeDto());
    }

    [HttpPatch("{id:guid}/deactivate/{tenantId:guid}")]
    public async Task<IActionResult> DeactivateTheme(Guid id, Guid tenantId, CancellationToken cancellationToken)
    {
        await _themeService.DeactivateThemeAsync(id, tenantId, cancellationToken);
        return NoContent();
    }

    [HttpGet("{tenantId:guid}/active")]
    public async Task<ActionResult<TenantThemeDto>> GetActiveTheme(Guid tenantId, CancellationToken cancellationToken)
    {
        var tenantTheme = await _themeService.GetActiveThemeAsync(tenantId, cancellationToken);
        if (tenantTheme is null)
        {
            return NotFound();
        }

        return Ok(tenantTheme.ToTenantThemeDto());
    }

    [HttpPost("{id:guid}/sections")]
    public async Task<ActionResult<IEnumerable<ThemeSectionDto>>> UpsertSections(Guid id, [FromBody] ThemeSectionsRequest request, CancellationToken cancellationToken)
    {
        if (request.Sections is null)
        {
            return BadRequest("Sections payload is required.");
        }

        var definitions = request.Sections
            .Select(s => new ThemeSectionDefinition(s.SectionName, JsonSerializer.Serialize(s.Configuration), s.SortOrder));

        var sections = await _themeService.UpsertSectionsAsync(id, definitions, cancellationToken);
        return Ok(sections.Select(s => new ThemeSectionDto(s.Id, s.SectionName, s.JsonConfig, s.SortOrder)));
    }

    public record ThemeUploadRequest(IFormFile Package);

    public record ThemeSectionsRequest(IReadOnlyList<ThemeSectionRequest> Sections);

    public record ThemeSectionRequest(string SectionName, JsonElement Configuration, int SortOrder);
}
