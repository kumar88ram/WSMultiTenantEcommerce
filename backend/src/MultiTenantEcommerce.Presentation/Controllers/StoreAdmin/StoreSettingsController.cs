using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("store-admin/settings")]
[Authorize(Roles = "StoreAdmin")]
public class StoreSettingsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public StoreSettingsController(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [ProducesResponseType(typeof(StoreSettingsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> GetSettings(CancellationToken cancellationToken)
    {
        var settings = await _dbContext.StoreSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            return NoContent();
        }

        return Ok(MapToDto(settings));
    }

    [HttpPut]
    [ProducesResponseType(typeof(StoreSettingsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertSettings([FromBody] UpdateStoreSettingsRequest request, CancellationToken cancellationToken)
    {
        var settings = await _dbContext.StoreSettings
            .FirstOrDefaultAsync(cancellationToken);

        if (settings is null)
        {
            settings = new StoreSetting
            {
                TenantId = _tenantResolver.CurrentTenantId,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.StoreSettings.Add(settings);
        }

        settings.LogoUrl = request.LogoUrl;
        settings.Title = request.Title;
        settings.Currency = request.Currency;
        settings.Timezone = request.Timezone;
        settings.MetaTitle = request.SeoMeta.MetaTitle;
        settings.MetaDescription = request.SeoMeta.MetaDescription;
        settings.MetaKeywords = request.SeoMeta.MetaKeywords;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(settings));
    }

    private static StoreSettingsDto MapToDto(StoreSetting entity)
    {
        return new StoreSettingsDto(
            entity.Id,
            entity.LogoUrl,
            entity.Title,
            entity.Currency,
            entity.Timezone,
            new SeoMetaDto(entity.MetaTitle, entity.MetaDescription, entity.MetaKeywords));
    }
}
