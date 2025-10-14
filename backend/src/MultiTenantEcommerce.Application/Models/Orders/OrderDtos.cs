using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models.Orders;

public record CartDto(
    Guid Id,
    Guid TenantId,
    Guid? UserId,
    string? GuestToken,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    IReadOnlyList<CartItemDto> Items,
    decimal Subtotal);

public record CartItemDto(
    Guid Id,
    Guid ProductId,
    Guid? ProductVariantId,
    string Name,
    string? Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record AddCartItemRequest(
    Guid? CartId,
    Guid? UserId,
    string? GuestToken,
    Guid ProductId,
    Guid? ProductVariantId,
    int Quantity);

public record CheckoutRequest(
    Guid? CartId,
    Guid? UserId,
    string? GuestToken,
    string Email,
    string ShippingAddress,
    string BillingAddress,
    string Currency,
    string? CouponCode,
    string PaymentProvider,
    IReadOnlyDictionary<string, string>? PaymentMetadata,
    decimal? ShippingAmount = null,
    string? ShippingMethodId = null,
    string? PaymentMethodId = null);

public record CheckoutResponse(OrderDto Order, PaymentIntentDto PaymentIntent);

public record OrderDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal Subtotal,
    decimal DiscountTotal,
    decimal TaxTotal,
    decimal ShippingTotal,
    decimal GrandTotal,
    string Currency,
    string Email,
    string ShippingAddress,
    string BillingAddress,
    string? CouponCode,
    DateTime CreatedAt,
    DateTime? PaidAt,
    DateTime? ShippedAt,
    DateTime? DeliveredAt,
    IReadOnlyList<OrderItemDto> Items,
    IReadOnlyList<PaymentTransactionDto> Payments);

public record OrderItemDto(
    Guid Id,
    Guid ProductId,
    Guid? ProductVariantId,
    string Name,
    string? Sku,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);

public record PaymentTransactionDto(
    Guid Id,
    string Provider,
    string ProviderReference,
    decimal Amount,
    string Currency,
    PaymentStatus Status,
    DateTime ProcessedAt);

public record PaymentIntentDto(
    string Provider,
    string ClientSecret,
    string PaymentUrl,
    IReadOnlyDictionary<string, string> Metadata);

public record PaymentWebhookRequest(
    string Provider,
    string EventType,
    string ProviderReference,
    PaymentStatus Status,
    decimal Amount,
    string Currency,
    string? Payload);

public record OrderListQuery(int Page = 1, int PageSize = 25, OrderStatus? Status = null, DateTime? From = null, DateTime? To = null, string? Search = null);

public record OrderListResult(int Page, int PageSize, int TotalCount, IReadOnlyList<OrderSummaryDto> Items);

public record OrderSummaryDto(
    Guid Id,
    string OrderNumber,
    OrderStatus Status,
    decimal GrandTotal,
    string Currency,
    string Email,
    DateTime CreatedAt,
    DateTime? PaidAt);

public record UpdateOrderStatusRequest(OrderStatus Status);

public record RefundRequest(decimal Amount, string Reason);

public enum OrderEmailNotificationType
{
    OrderPlaced,
    OrderShipped
}

public record OrderEmailNotification(
    OrderEmailNotificationType Type,
    Guid TenantId,
    string Email,
    string OrderNumber,
    decimal GrandTotal,
    string Currency,
    DateTime CreatedAt,
    string? TrackingNumber);
