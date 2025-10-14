using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPaymentGateway
{
    string Provider { get; }

    Task<PaymentIntentDto> PayAsync(
        Order order,
        PaymentGatewayContext context,
        CancellationToken cancellationToken = default);

    Task<PaymentVerificationResult> VerifyAsync(
        PaymentVerificationRequest request,
        PaymentGatewayContext context,
        CancellationToken cancellationToken = default);

    Task<PaymentStatus> RefundAsync(
        PaymentRefundRequest request,
        PaymentGatewayContext context,
        CancellationToken cancellationToken = default);
}
