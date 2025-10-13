using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/tenants")]
public class TenantsController : ControllerBase
{
    private readonly ITenantProvisioningService _tenantProvisioningService;

    public TenantsController(ITenantProvisioningService tenantProvisioningService)
    {
        _tenantProvisioningService = tenantProvisioningService;
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
            return Created($"/admin/tenants/{response.Id}", response);
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
}
