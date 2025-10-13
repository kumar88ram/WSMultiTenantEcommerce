using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("store-admin/menus")]
[Authorize(Roles = "StoreAdmin")]
public class MenuBuilderController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public MenuBuilderController(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<MenuDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMenus(CancellationToken cancellationToken)
    {
        var menus = await _dbContext.MenuDefinitions
            .AsNoTracking()
            .OrderBy(menu => menu.Name)
            .ToListAsync(cancellationToken);

        return Ok(menus.Select(MapToDto));
    }

    [HttpGet("sample")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetSampleSchema()
    {
        return Ok(MenuBuilderSamples.SampleMenuJson);
    }

    [HttpPut]
    [ProducesResponseType(typeof(MenuDefinitionDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpsertMenu([FromBody] UpsertMenuDefinitionRequest request, CancellationToken cancellationToken)
    {
        var existing = await _dbContext.MenuDefinitions
            .FirstOrDefaultAsync(menu => menu.Name == request.Name, cancellationToken);

        if (existing is null)
        {
            existing = new MenuDefinition
            {
                TenantId = _tenantResolver.CurrentTenantId,
                Name = request.Name,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.MenuDefinitions.Add(existing);
        }

        existing.TreeJson = request.Tree.GetRawText();
        existing.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(existing));
    }

    private static MenuDefinitionDto MapToDto(MenuDefinition definition)
    {
        var json = string.IsNullOrWhiteSpace(definition.TreeJson) ? "{}" : definition.TreeJson;
        using var document = JsonDocument.Parse(json);
        return new MenuDefinitionDto(definition.Id, definition.Name, document.RootElement.Clone());
    }
}
