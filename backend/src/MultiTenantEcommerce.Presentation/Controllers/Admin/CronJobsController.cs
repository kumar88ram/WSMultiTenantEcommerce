using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/cron-jobs")]
[Authorize(Roles = "SuperAdmin")]
public class CronJobsController : ControllerBase
{
    private readonly ICronJobService _cronJobService;

    public CronJobsController(ICronJobService cronJobService)
    {
        _cronJobService = cronJobService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<CronJobResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCronJobs(CancellationToken cancellationToken)
    {
        var cronJobs = await _cronJobService.GetAsync(cancellationToken);
        return Ok(cronJobs);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(CronJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCronJob(Guid id, CancellationToken cancellationToken)
    {
        var cronJob = await _cronJobService.GetByIdAsync(id, cancellationToken);
        return cronJob is null ? NotFound() : Ok(cronJob);
    }

    [HttpPost]
    [ProducesResponseType(typeof(CronJobResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateCronJob([FromBody] CronJobRequest request, CancellationToken cancellationToken)
    {
        var cronJob = await _cronJobService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCronJob), new { id = cronJob.Id }, cronJob);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(CronJobResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateCronJob(Guid id, [FromBody] CronJobRequest request, CancellationToken cancellationToken)
    {
        var cronJob = await _cronJobService.UpdateAsync(id, request, cancellationToken);
        return cronJob is null ? NotFound() : Ok(cronJob);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCronJob(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _cronJobService.DeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
