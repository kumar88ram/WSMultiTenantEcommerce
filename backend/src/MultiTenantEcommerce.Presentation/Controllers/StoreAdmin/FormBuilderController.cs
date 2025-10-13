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
[Route("store-admin/forms")]
[Authorize(Roles = "StoreAdmin")]
public class FormBuilderController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public FormBuilderController(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<FormDefinitionDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForms(CancellationToken cancellationToken)
    {
        var forms = await _dbContext.FormDefinitions
            .AsNoTracking()
            .Include(form => form.Fields)
            .OrderBy(form => form.Name)
            .ToListAsync(cancellationToken);

        return Ok(forms.Select(MapToDto));
    }

    [HttpGet("sample")]
    [ProducesResponseType(typeof(string), StatusCodes.Status200OK)]
    public IActionResult GetSampleForm()
    {
        return Ok(FormBuilderSamples.SampleFormJson);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(FormDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForm(Guid id, CancellationToken cancellationToken)
    {
        var form = await _dbContext.FormDefinitions
            .AsNoTracking()
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (form is null)
        {
            return NotFound();
        }

        return Ok(MapToDto(form));
    }

    [HttpPost]
    [ProducesResponseType(typeof(FormDefinitionDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateForm([FromBody] SaveFormDefinitionRequest request, CancellationToken cancellationToken)
    {
        var form = new FormDefinition
        {
            TenantId = _tenantResolver.CurrentTenantId,
            Name = request.Name,
            Description = request.Description,
            CreatedAt = DateTime.UtcNow
        };

        form.Fields = MapFields(request.Fields, form.Id, form.TenantId).ToList();

        _dbContext.FormDefinitions.Add(form);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetForm), new { id = form.Id }, MapToDto(form));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(FormDefinitionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateForm(Guid id, [FromBody] SaveFormDefinitionRequest request, CancellationToken cancellationToken)
    {
        var form = await _dbContext.FormDefinitions
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (form is null)
        {
            return NotFound();
        }

        form.Name = request.Name;
        form.Description = request.Description;
        form.UpdatedAt = DateTime.UtcNow;

        _dbContext.FormFields.RemoveRange(form.Fields);
        form.Fields = MapFields(request.Fields, form.Id, form.TenantId).ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(form));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteForm(Guid id, CancellationToken cancellationToken)
    {
        var form = await _dbContext.FormDefinitions
            .Include(f => f.Fields)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        if (form is null)
        {
            return NotFound();
        }

        _dbContext.FormFields.RemoveRange(form.Fields);
        _dbContext.FormDefinitions.Remove(form);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private static IEnumerable<FormField> MapFields(IEnumerable<FormFieldDto> fields, Guid formId, Guid tenantId)
    {
        if (fields is null)
        {
            yield break;
        }

        foreach (var field in fields)
        {
            yield return new FormField
            {
                TenantId = tenantId,
                FormDefinitionId = formId,
                Label = field.Label,
                Name = field.Name,
                Type = field.Type.ToLowerInvariant(),
                IsRequired = field.IsRequired,
                Placeholder = field.Placeholder,
                OptionsJson = field.Options is null ? null : JsonSerializer.Serialize(field.Options),
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    private static FormDefinitionDto MapToDto(FormDefinition form)
    {
        var fields = form.Fields
            .OrderBy(field => field.CreatedAt)
            .Select(field => new FormFieldDto(
                field.Id,
                field.Label,
                field.Name,
                field.Type,
                field.IsRequired,
                field.Placeholder,
                string.IsNullOrWhiteSpace(field.OptionsJson)
                    ? null
                    : JsonSerializer.Deserialize<List<string>>(field.OptionsJson)))
            .ToList();

        return new FormDefinitionDto(form.Id, form.Name, form.Description, fields);
    }
}
