using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/plans")]
[Authorize(Roles = "SuperAdmin")]
public class PlansController : ControllerBase
{
    private readonly ISubscriptionPlanService _planService;

    public PlansController(ISubscriptionPlanService planService)
    {
        _planService = planService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionPlanResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPlans(CancellationToken cancellationToken)
    {
        var plans = await _planService.GetAsync(cancellationToken);
        return Ok(plans);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPlan(Guid id, CancellationToken cancellationToken)
    {
        var plan = await _planService.GetByIdAsync(id, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePlan([FromBody] SubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPlan), new { id = plan.Id }, plan);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionPlanResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePlan(Guid id, [FromBody] SubscriptionPlanRequest request, CancellationToken cancellationToken)
    {
        var plan = await _planService.UpdateAsync(id, request, cancellationToken);
        return plan is null ? NotFound() : Ok(plan);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeletePlan(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var deleted = await _planService.DeleteAsync(id, cancellationToken);
            if (!deleted)
            {
                return NotFound();
            }
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { error = ex.Message });
        }

        return NoContent();
    }
}
