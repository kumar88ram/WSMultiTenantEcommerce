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
[Route("store-admin/widgets")]
[Authorize(Roles = "StoreAdmin")]
public class WidgetsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public WidgetsController(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<WidgetDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWidgets(CancellationToken cancellationToken)
    {
        var widgets = await _dbContext.WidgetDefinitions
            .AsNoTracking()
            .OrderBy(widget => widget.Name)
            .ToListAsync(cancellationToken);

        return Ok(widgets.Select(MapToDto));
    }

    [HttpGet("sample")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetSampleWidget()
    {
        return Ok(WidgetSamples.SampleWidgetJson);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(WidgetDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWidget(Guid id, CancellationToken cancellationToken)
    {
        var widget = await _dbContext.WidgetDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.Id == id, cancellationToken);

        if (widget is null)
        {
            return NotFound();
        }

        return Ok(MapToDto(widget));
    }

    [HttpPost]
    [ProducesResponseType(typeof(WidgetDefinitionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateWidget([FromBody] SaveWidgetDefinitionRequest request, CancellationToken cancellationToken)
    {
        var widget = new WidgetDefinition
        {
            TenantId = _tenantResolver.CurrentTenantId,
            Name = request.Name,
            Type = request.Type,
            ConfigJson = request.Config.GetRawText(),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.WidgetDefinitions.Add(widget);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetWidget), new { id = widget.Id }, MapToDto(widget));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(WidgetDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateWidget(Guid id, [FromBody] SaveWidgetDefinitionRequest request, CancellationToken cancellationToken)
    {
        var widget = await _dbContext.WidgetDefinitions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (widget is null)
        {
            return NotFound();
        }

        widget.Name = request.Name;
        widget.Type = request.Type;
        widget.ConfigJson = request.Config.GetRawText();
        widget.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(widget));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteWidget(Guid id, CancellationToken cancellationToken)
    {
        var widget = await _dbContext.WidgetDefinitions.FirstOrDefaultAsync(w => w.Id == id, cancellationToken);
        if (widget is null)
        {
            return NotFound();
        }

        _dbContext.WidgetDefinitions.Remove(widget);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static WidgetDefinitionDto MapToDto(WidgetDefinition widget)
    {
        var json = string.IsNullOrWhiteSpace(widget.ConfigJson) ? "{}" : widget.ConfigJson;
        using var document = JsonDocument.Parse(json);
        return new WidgetDefinitionDto(widget.Id, widget.Name, widget.Type, document.RootElement.Clone());
    }
}
