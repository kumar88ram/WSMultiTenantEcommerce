using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IOtpService
{
    Task RequestOtpAsync(User user, string purpose, CancellationToken cancellationToken = default);
    Task<bool> ValidateOtpAsync(User user, string code, string purpose, CancellationToken cancellationToken = default);
}
