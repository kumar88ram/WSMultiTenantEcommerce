using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Support;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("api/store-admin/support-tickets")]
[Authorize]
public class SupportTicketsController : ControllerBase
{
    private readonly ISupportTicketService _supportTicketService;

    public SupportTicketsController(ISupportTicketService supportTicketService)
    {
        _supportTicketService = supportTicketService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SupportTicketListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTickets([FromQuery] SupportTicketQueryParameters parameters, CancellationToken cancellationToken)
    {
        var query = new SupportTicketListQuery(
            parameters.Page,
            parameters.PageSize,
            parameters.Status,
            parameters.AssignedToUserId,
            parameters.Search);

        var tickets = await _supportTicketService.GetAsync(query, cancellationToken);
        return Ok(tickets);
    }

    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketService.GetByIdAsync(ticketId, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTicket([FromBody] CreateSupportTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { ticketId = ticket.Id }, ticket);
    }

    [HttpPut("{ticketId:guid}")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTicket(Guid ticketId, [FromBody] UpdateSupportTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketService.UpdateAsync(ticketId, request, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{ticketId:guid}/messages")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage(Guid ticketId, [FromBody] AddSupportTicketMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _supportTicketService.AddMessageAsync(ticketId, request, cancellationToken);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public class SupportTicketQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public SupportTicketStatus? Status { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Search { get; set; }
    }
}
