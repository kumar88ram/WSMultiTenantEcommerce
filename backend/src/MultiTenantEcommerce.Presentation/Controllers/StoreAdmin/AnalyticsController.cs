using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("store-admin/analytics")]
[Authorize(Roles = "StoreAdmin")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("visits")]
    [ProducesResponseType(typeof(IReadOnlyList<AnalyticsVisitPoint>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnalyticsVisitPoint>>> GetVisits([FromQuery] DateRangeQuery query, CancellationToken cancellationToken)
    {
        var range = query.ToRange();
        var data = await _analyticsService.GetVisitSeriesAsync(range.From, range.To, cancellationToken);
        return Ok(data);
    }

    [HttpGet("sales")]
    [ProducesResponseType(typeof(IReadOnlyList<AnalyticsSalesPoint>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnalyticsSalesPoint>>> GetSales([FromQuery] DateRangeQuery query, CancellationToken cancellationToken)
    {
        var range = query.ToRange();
        var data = await _analyticsService.GetSalesByDateAsync(range.From, range.To, cancellationToken);
        return Ok(data);
    }

    [HttpGet("conversion")]
    [ProducesResponseType(typeof(IReadOnlyList<ConversionRatePoint>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ConversionRatePoint>>> GetConversionRates([FromQuery] DateRangeQuery query, CancellationToken cancellationToken)
    {
        var range = query.ToRange();
        var data = await _analyticsService.GetConversionRatesAsync(range.From, range.To, cancellationToken);
        return Ok(data);
    }

    [HttpGet("events/sample")]
    [ProducesResponseType(typeof(IReadOnlyList<AnalyticsEventDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<AnalyticsEventDto>>> GetSampleEvents([FromQuery] int take = 25, CancellationToken cancellationToken = default)
    {
        var normalizedTake = take <= 0 ? 25 : Math.Min(take, 250);
        var events = await _analyticsService.GetSampleEventsAsync(normalizedTake, cancellationToken);
        return Ok(events);
    }

    public class DateRangeQuery
    {
        public DateOnly? From { get; set; }
        public DateOnly? To { get; set; }

        public (DateOnly From, DateOnly To) ToRange()
        {
            var to = To ?? DateOnly.FromDateTime(DateTime.UtcNow);
            var from = From ?? to.AddDays(-29);
            return from <= to ? (from, to) : (to, from);
        }
    }
}
