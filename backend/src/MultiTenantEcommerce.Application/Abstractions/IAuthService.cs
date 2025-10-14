using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default);
    Task RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken = default);
    Task<AuthResponse> SocialLoginAsync(SocialLoginProvider provider, SocialLoginRequest request, CancellationToken cancellationToken = default);
}
