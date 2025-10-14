using Microsoft.AspNetCore.Mvc;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Support;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Presentation.Controllers.Storefront;

[ApiController]
[Route("store/{tenant}/support-tickets")]
public class SupportTicketsController : ControllerBase
{
    private readonly ISupportTicketService _supportTicketService;
    private readonly ITenantResolver _tenantResolver;

    public SupportTicketsController(ISupportTicketService supportTicketService, ITenantResolver tenantResolver)
    {
        _supportTicketService = supportTicketService;
        _tenantResolver = tenantResolver;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateTicket(string tenant, [FromBody] CreateCustomerTicketRequest request, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);

        var ticketRequest = new CreateSupportTicketRequest(
            request.Subject,
            request.Message,
            SupportTicketActorType.Customer,
            request.CustomerName,
            request.CustomerEmail,
            null,
            request.Attachments,
            request.Description);

        var ticket = await _supportTicketService.CreateAsync(ticketRequest, cancellationToken);
        return CreatedAtAction(nameof(GetTicket), new { tenant, ticketId = ticket.Id, email = request.CustomerEmail }, ticket);
    }

    [HttpGet("{ticketId:guid}")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTicket(string tenant, Guid ticketId, [FromQuery] string email, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        if (string.IsNullOrWhiteSpace(email))
        {
            return BadRequest(new { error = "Email is required" });
        }

        var hasAccess = await _supportTicketService.ValidateCustomerAccessAsync(new CustomerTicketLookup(ticketId, email), cancellationToken);
        if (!hasAccess)
        {
            return NotFound();
        }

        var ticket = await _supportTicketService.GetByIdAsync(ticketId, cancellationToken);
        return ticket is null ? NotFound() : Ok(ticket);
    }

    [HttpPost("{ticketId:guid}/messages")]
    [ProducesResponseType(typeof(SupportTicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMessage(string tenant, Guid ticketId, [FromBody] CustomerTicketMessageRequest request, CancellationToken cancellationToken)
    {
        EnsureTenantContext(tenant);
        if (string.IsNullOrWhiteSpace(request.CustomerEmail))
        {
            return BadRequest(new { error = "Email is required" });
        }

        var hasAccess = await _supportTicketService.ValidateCustomerAccessAsync(new CustomerTicketLookup(ticketId, request.CustomerEmail), cancellationToken);
        if (!hasAccess)
        {
            return NotFound();
        }

        var messageRequest = new AddSupportTicketMessageRequest(
            request.Body,
            SupportTicketActorType.Customer,
            null,
            request.CustomerName,
            request.CustomerEmail,
            request.Attachments);

        try
        {
            var ticket = await _supportTicketService.AddMessageAsync(ticketId, messageRequest, cancellationToken);
            return ticket is null ? NotFound() : Ok(ticket);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    private void EnsureTenantContext(string tenant)
    {
        if (!string.IsNullOrWhiteSpace(_tenantResolver.TenantIdentifier) &&
            !string.Equals(_tenantResolver.TenantIdentifier, tenant, StringComparison.OrdinalIgnoreCase))
        {
            Response.Headers["X-Tenant-Mismatch"] = _tenantResolver.TenantIdentifier;
        }
    }

    public record CreateCustomerTicketRequest(
        string Subject,
        string Message,
        string? CustomerName,
        string CustomerEmail,
        IReadOnlyList<SupportTicketAttachmentInput>? Attachments = null,
        string? Description = null);

    public record CustomerTicketMessageRequest(
        string Body,
        string? CustomerName,
        string CustomerEmail,
        IReadOnlyList<SupportTicketAttachmentInput>? Attachments = null);
}
