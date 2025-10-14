using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models.Support;

public record SupportTicketAttachmentInput(string FileName, string Url);

public record CreateSupportTicketRequest(
    string Subject,
    string Message,
    SupportTicketActorType AuthorType,
    string? CustomerName,
    string? CustomerEmail,
    Guid? CreatedByUserId,
    IReadOnlyList<SupportTicketAttachmentInput>? Attachments = null,
    string? Description = null);

public record UpdateSupportTicketRequest(
    string? Subject = null,
    SupportTicketStatus? Status = null,
    Guid? AssignedToUserId = null,
    string? Description = null);

public record AddSupportTicketMessageRequest(
    string Body,
    SupportTicketActorType AuthorType,
    Guid? AuthorUserId,
    string? AuthorName,
    string? AuthorEmail,
    IReadOnlyList<SupportTicketAttachmentInput>? Attachments = null);

public record SupportTicketAttachmentDto(Guid Id, string FileName, string Url);

public record SupportTicketMessageDto(
    Guid Id,
    string Body,
    SupportTicketActorType AuthorType,
    Guid? AuthorUserId,
    string? AuthorName,
    string? AuthorEmail,
    DateTime CreatedAt,
    IReadOnlyList<SupportTicketAttachmentDto> Attachments);

public record SupportTicketDto(
    Guid Id,
    Guid TenantId,
    string? TenantName,
    string Subject,
    string? Description,
    SupportTicketStatus Status,
    Guid? AssignedToUserId,
    string? AssignedToUserName,
    Guid? CreatedByUserId,
    string? CustomerName,
    string? CustomerEmail,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    IReadOnlyList<SupportTicketMessageDto> Messages);

public record SupportTicketSummaryDto(
    Guid Id,
    Guid TenantId,
    string? TenantName,
    string Subject,
    SupportTicketStatus Status,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    string? AssignedToUserName,
    string? CustomerName,
    DateTime? LastMessageAt);

public record SupportTicketListQuery(
    int Page = 1,
    int PageSize = 25,
    SupportTicketStatus? Status = null,
    Guid? AssignedToUserId = null,
    string? Search = null);

public record SupportTicketListResult(
    int Page,
    int PageSize,
    int TotalCount,
    IReadOnlyList<SupportTicketSummaryDto> Items);

public record CustomerTicketLookup(Guid TicketId, string CustomerEmail);
