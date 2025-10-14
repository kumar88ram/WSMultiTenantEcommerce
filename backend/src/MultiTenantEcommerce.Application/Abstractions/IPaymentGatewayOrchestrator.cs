using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPaymentGatewayOrchestrator
{
    Task<PaymentIntentDto> PayAsync(
        string provider,
        Order order,
        CancellationToken cancellationToken = default);

    Task<PaymentVerificationResult> VerifyAsync(
        string provider,
        PaymentVerificationRequest request,
        CancellationToken cancellationToken = default);

    Task<PaymentStatus> RefundAsync(
        string provider,
        PaymentRefundRequest request,
        CancellationToken cancellationToken = default);
}
