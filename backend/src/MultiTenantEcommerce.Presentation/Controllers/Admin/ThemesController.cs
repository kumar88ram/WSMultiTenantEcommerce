using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Application.Services;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/themes")]
[Authorize(Roles = "SuperAdmin")]
public class ThemesController : ControllerBase
{
    private readonly IThemeService _themeService;
    private readonly ThemeBuilderService _themeBuilderService;
    private readonly IWebHostEnvironment _environment;
    private readonly IThemePreviewService _themePreviewService;
    private readonly IThemeCloneService _themeCloneService;
    private readonly IThemeAnalyticsService _themeAnalyticsService;
    private readonly ApplicationDbContext _dbContext;

    public ThemesController(
        IThemeService themeService,
        ThemeBuilderService themeBuilderService,
        IWebHostEnvironment environment,
        IThemePreviewService themePreviewService,
        IThemeCloneService themeCloneService,
        IThemeAnalyticsService themeAnalyticsService,
        ApplicationDbContext dbContext)
    {
        _themeService = themeService;
        _themeBuilderService = themeBuilderService;
        _environment = environment;
        _themePreviewService = themePreviewService;
        _themeCloneService = themeCloneService;
        _themeAnalyticsService = themeAnalyticsService;
        _dbContext = dbContext;
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

    [HttpGet("{themeId:guid}/preview-url")]
    public async Task<ActionResult<ThemePreviewResponse>> GetThemePreviewUrl(Guid themeId, CancellationToken cancellationToken)
    {
        var exists = await _dbContext.Themes.AsNoTracking().AnyAsync(t => t.Id == themeId, cancellationToken);
        if (!exists)
        {
            return NotFound();
        }

        var token = _themePreviewService.GeneratePreviewToken(themeId, 10);
        if (!_themePreviewService.TryValidateToken(token, out _, out var expiresAt))
        {
            expiresAt = DateTime.UtcNow.AddMinutes(10);
        }

        var previewUrl = _themePreviewService.BuildPreviewUrl(themeId, token);
        return Ok(new ThemePreviewResponse(previewUrl.ToString(), expiresAt));
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

    [HttpGet("{themeId:guid}/export")]
    public async Task<IActionResult> ExportTheme(Guid themeId, CancellationToken cancellationToken)
    {
        var theme = await _dbContext.Themes.FirstOrDefaultAsync(t => t.Id == themeId, cancellationToken);
        if (theme is null)
        {
            return NotFound();
        }

        var themeDirectory = Path.Combine(_environment.ContentRootPath, "themes", theme.Code);
        if (!Directory.Exists(themeDirectory))
        {
            return NotFound("Theme assets not found on disk.");
        }

        await using var memoryStream = new MemoryStream();
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var file in Directory.EnumerateFiles(themeDirectory, "*", SearchOption.AllDirectories))
            {
                var entryName = Path.GetRelativePath(themeDirectory, file).Replace(Path.DirectorySeparatorChar, '/');
                archive.CreateEntryFromFile(file, entryName, CompressionLevel.Fastest);
            }
        }

        memoryStream.Position = 0;
        var fileName = $"{theme.Code}-{DateTime.UtcNow:yyyyMMddHHmmss}.zip";

        await LogAuditAsync(theme.Id, "Export", cancellationToken);

        return File(memoryStream, "application/zip", fileName);
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

    [HttpPost("clone")]
    public async Task<ActionResult<ThemeCloneResponse>> CloneTheme([FromBody] ThemeCloneRequest request, CancellationToken cancellationToken)
    {
        if (request is null || request.SourceTenantId == Guid.Empty || request.TargetTenantId == Guid.Empty)
        {
            return BadRequest("Source and target tenant identifiers are required.");
        }

        if (request.SourceTenantId == request.TargetTenantId)
        {
            return BadRequest("Source and target tenants must be different.");
        }

        var adminId = GetCurrentAdminId();
        if (adminId == Guid.Empty)
        {
            return Forbid();
        }

        var clonedThemeId = await _themeCloneService.CloneTenantThemeAsync(request.SourceTenantId, request.TargetTenantId, adminId, cancellationToken);
        if (clonedThemeId is null)
        {
            return NotFound("Source tenant does not have an active theme to clone.");
        }

        return Ok(new ThemeCloneResponse(clonedThemeId.Value));
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<IEnumerable<ThemeUsageSummaryDto>>> GetThemeAnalytics(CancellationToken cancellationToken)
    {
        var analytics = await _themeAnalyticsService.GetThemeAnalyticsAsync(cancellationToken);
        return Ok(analytics);
    }

    [HttpGet("{id:guid}/usage")]
    public async Task<ActionResult<IEnumerable<TenantThemeUsageDto>>> GetThemeUsage(Guid id, CancellationToken cancellationToken)
    {
        var usage = await _themeAnalyticsService.GetThemeUsageAsync(id, cancellationToken);
        return Ok(usage);
    }

    public record ThemeUploadRequest(IFormFile Package);

    public record ThemeSectionsRequest(IReadOnlyList<ThemeSectionRequest> Sections);

    public record ThemeSectionRequest(string SectionName, JsonElement Configuration, int SortOrder);

    public record ThemePreviewResponse(string PreviewUrl, DateTime ExpiresAt);

    public record ThemeCloneRequest(Guid SourceTenantId, Guid TargetTenantId);

    public record ThemeCloneResponse(Guid TenantThemeId);

    private Guid GetCurrentAdminId()
    {
        var claimValue = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.TryParse(claimValue, out var adminId) ? adminId : Guid.Empty;
    }

    private async Task LogAuditAsync(Guid themeId, string action, CancellationToken cancellationToken)
    {
        var adminId = GetCurrentAdminId();
        if (adminId == Guid.Empty)
        {
            return;
        }

        await _dbContext.ThemeAuditLogs.AddAsync(new ThemeAuditLog
        {
            ThemeId = themeId,
            AdminId = adminId,
            Action = action,
            Timestamp = DateTime.UtcNow
        }, cancellationToken);

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
