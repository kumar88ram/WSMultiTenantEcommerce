using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ICheckoutService
{
    Task<CartDto> GetOrCreateCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken = default);
    Task<CartDto> AddItemToCartAsync(AddCartItemRequest request, CancellationToken cancellationToken = default);
    Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default);
    Task<OrderDto?> HandlePaymentWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default);
    Task<OrderListResult> GetOrdersAsync(OrderListQuery query, CancellationToken cancellationToken = default);
    Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? trackingNumber, CancellationToken cancellationToken = default);
    Task<OrderDto> RefundOrderAsync(Guid orderId, RefundRequest request, CancellationToken cancellationToken = default);
}
