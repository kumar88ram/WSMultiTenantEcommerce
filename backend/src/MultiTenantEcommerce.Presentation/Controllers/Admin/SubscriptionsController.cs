using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/subscriptions")]
[Authorize(Roles = "SuperAdmin")]
public class SubscriptionsController : ControllerBase
{
    private readonly ISubscriptionService _subscriptionService;

    public SubscriptionsController(ISubscriptionService subscriptionService)
    {
        _subscriptionService = subscriptionService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<SubscriptionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSubscriptions(CancellationToken cancellationToken)
    {
        var subscriptions = await _subscriptionService.GetAsync(cancellationToken);
        return Ok(subscriptions);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSubscription(Guid id, CancellationToken cancellationToken)
    {
        var subscription = await _subscriptionService.GetByIdAsync(id, cancellationToken);
        return subscription is null ? NotFound() : Ok(subscription);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSubscription([FromBody] SubscriptionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionService.CreateAsync(request, cancellationToken);
            return CreatedAtAction(nameof(GetSubscription), new { id = subscription.Id }, subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(SubscriptionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSubscription(Guid id, [FromBody] SubscriptionRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var subscription = await _subscriptionService.UpdateAsync(id, request, cancellationToken);
            return subscription is null ? NotFound() : Ok(subscription);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSubscription(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _subscriptionService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
