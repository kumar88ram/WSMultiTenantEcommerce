using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SampleController : ControllerBase
{
    private readonly ITenantResolver _tenantResolver;

    public SampleController(ITenantResolver tenantResolver)
    {
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new { message = "Protected resource", tenant = _tenantResolver.CurrentTenantId });
    }
}
