using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPaymentGatewayClient
{
    Task<PaymentIntentDto> CreatePaymentIntentAsync(Order order, CancellationToken cancellationToken = default);
    PaymentStatus TranslateStatus(string providerStatus);
}
