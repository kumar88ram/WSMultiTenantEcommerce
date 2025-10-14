using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models.Orders;

public record RefundRequestItemInput(Guid OrderItemId, int Quantity);

public record CreateRefundRequestDto(
    Guid OrderId,
    string Reason,
    IReadOnlyList<RefundRequestItemInput> Items);

public record RefundDecisionRequest(string? Notes);

public record RefundRequestDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    RefundRequestStatus Status,
    string Reason,
    decimal RequestedAmount,
    decimal? ApprovedAmount,
    DateTime CreatedAt,
    DateTime? DecisionAt,
    DateTime? ProcessedAt,
    IReadOnlyList<RefundRequestItemDto> Items,
    Guid? PaymentTransactionId,
    string Currency);

public record RefundRequestItemDto(
    Guid OrderItemId,
    string Name,
    int Quantity,
    decimal LineTotal);

public record RefundRequestSummaryDto(
    Guid Id,
    Guid OrderId,
    string OrderNumber,
    RefundRequestStatus Status,
    decimal RequestedAmount,
    decimal? ApprovedAmount,
    DateTime CreatedAt,
    DateTime? DecisionAt);

public record RefundRequestListQuery(int Page = 1, int PageSize = 25, RefundRequestStatus? Status = null);

public record RefundRequestListResult(int Page, int PageSize, int TotalCount, IReadOnlyList<RefundRequestSummaryDto> Items);
