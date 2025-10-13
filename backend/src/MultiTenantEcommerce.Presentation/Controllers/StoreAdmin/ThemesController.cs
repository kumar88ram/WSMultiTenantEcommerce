using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("store-admin/themes")]
[Authorize(Roles = "StoreAdmin")]
public class ThemesController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public ThemesController(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ThemeDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetThemes(CancellationToken cancellationToken)
    {
        var themes = await _dbContext.Themes
            .AsNoTracking()
            .OrderByDescending(t => t.AppliedAt)
            .ToListAsync(cancellationToken);

        return Ok(themes.Select(MapToDto));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ThemeDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTheme([FromBody] CreateThemeRequest request, CancellationToken cancellationToken)
    {
        var theme = new Theme
        {
            TenantId = _tenantResolver.CurrentTenantId,
            Name = request.Name,
            DisplayName = request.DisplayName,
            Description = request.Description,
            PreviewImageUrl = request.PreviewImageUrl,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Themes.Add(theme);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetThemes), null, MapToDto(theme));
    }

    [HttpPost("{id:guid}/apply")]
    [ProducesResponseType(typeof(ThemeDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApplyTheme(Guid id, CancellationToken cancellationToken)
    {
        var theme = await _dbContext.Themes.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (theme is null)
        {
            return NotFound();
        }

        var tenantThemes = await _dbContext.Themes.ToListAsync(cancellationToken);
        foreach (var tenantTheme in tenantThemes)
        {
            tenantTheme.IsActive = tenantTheme.Id == theme.Id;
            tenantTheme.AppliedAt = tenantTheme.IsActive ? DateTime.UtcNow : tenantTheme.AppliedAt;
            tenantTheme.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(theme));
    }

    private static ThemeDto MapToDto(Theme entity)
    {
        return new ThemeDto(
            entity.Id,
            entity.Name,
            entity.DisplayName,
            entity.Description,
            entity.PreviewImageUrl,
            entity.IsActive,
            entity.AppliedAt);
    }
}
