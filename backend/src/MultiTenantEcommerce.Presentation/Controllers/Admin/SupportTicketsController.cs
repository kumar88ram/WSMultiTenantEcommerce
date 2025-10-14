using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Support;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Presentation.Controllers.Admin;

[ApiController]
[Route("admin/support-tickets")]
[Authorize(Roles = "SuperAdmin")]
public class SupportTicketsController : ControllerBase
{
    private readonly ISupportTicketAdminService _supportTicketAdminService;

    public SupportTicketsController(ISupportTicketAdminService supportTicketAdminService)
    {
        _supportTicketAdminService = supportTicketAdminService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(SupportTicketListResult), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTickets([FromQuery] AdminSupportTicketQuery parameters, CancellationToken cancellationToken)
    {
        var query = new SupportTicketListQuery(parameters.Page, parameters.PageSize, parameters.Status, parameters.AssignedToUserId, parameters.Search);
        var result = await _supportTicketAdminService.GetAsync(parameters.TenantId, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{tenantId:guid}/{ticketId:guid}")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(Guid tenantId, Guid ticketId, CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketAdminService.GetByIdAsync(tenantId, ticketId, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPut("{tenantId:guid}/{ticketId:guid}")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTicket(Guid tenantId, Guid ticketId, [FromBody] UpdateSupportTicketRequest request, CancellationToken cancellationToken)
    {
        var ticket = await _supportTicketAdminService.UpdateAsync(tenantId, ticketId, request, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{tenantId:guid}/{ticketId:guid}/messages")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage(Guid tenantId, Guid ticketId, [FromBody] AddSupportTicketMessageRequest request, CancellationToken cancellationToken)
    {
        try
        {
            var ticket = await _supportTicketAdminService.AddMessageAsync(tenantId, ticketId, request, cancellationToken);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    public class AdminSupportTicketQuery
    {
        public Guid? TenantId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public SupportTicketStatus? Status { get; set; }
        public Guid? AssignedToUserId { get; set; }
        public string? Search { get; set; }
    }
}
