using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/site-analytics")]
[Authorize(Roles = "SuperAdmin")]
public class SiteAnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public SiteAnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SiteAnalyticsResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        var summary = await _analyticsService.GetSiteSummaryAsync(cancellationToken);
        return Ok(summary);
    }
}
