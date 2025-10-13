using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/tenants")]
[Authorize(Roles = "SuperAdmin")]
public class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _tenantProvisioningService;
    private readonly IAdminTenantService _adminTenantService;

    public TenantsController(
        ITenantProvisioningService tenantProvisioningService,
        IAdminTenantService adminTenantService)
    {
        _tenantProvisioningService = tenantProvisioningService;
        _adminTenantService = adminTenantService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<TenantResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTenants(CancellationToken cancellationToken)
    {
        var tenants = await _adminTenantService.GetAsync(cancellationToken);
        return Ok(tenants);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTenant(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await _adminTenantService.GetByIdAsync(id, cancellationToken);
        if (tenant is null)
        {
            return NotFound();
        }

        return Ok(tenant);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> CreateTenant([FromBody] CreateTenantRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var tenant = await _tenantProvisioningService.CreateTenantAsync(request, cancellationToken);
            var response = TenantResponse.FromEntity(tenant);
            return CreatedAtAction(nameof(GetTenant), new { id = response.Id }, response);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTenant(Guid id, [FromBody] UpdateTenantRequest request, CancellationToken cancellationToken)
    {
        var updatedTenant = await _adminTenantService.UpdateAsync(id, request, cancellationToken);
        if (updatedTenant is null)
        {
            return NotFound();
        }

        return Ok(updatedTenant);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTenant(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _adminTenantService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
