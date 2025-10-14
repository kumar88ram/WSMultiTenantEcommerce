using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;
using MultiTenantEcommerce.Infrastructure.Security;

namespace MultiTenantEcommerce.Application.Services;

public class AuthService : IAuthService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITokenFactory _tokenFactory;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<AuthService> _logger;
    private readonly IOtpService _otpService;
    private readonly IEmailSender _emailSender;
    private readonly PasswordResetOptions _passwordResetOptions;

    public AuthService(
        ApplicationDbContext dbContext,
        ITokenFactory tokenFactory,
        IPasswordHasher passwordHasher,
        ITenantResolver tenantResolver,
        IOtpService otpService,
        IEmailSender emailSender,
        IOptions<PasswordResetOptions> passwordResetOptions,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenFactory = tokenFactory;
        _passwordHasher = passwordHasher;
        _tenantResolver = tenantResolver;
        _otpService = otpService;
        _emailSender = emailSender;
        _passwordResetOptions = passwordResetOptions.Value;
        _logger = logger;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var normalizedUserName = request.UserName.ToUpperInvariant();

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.NormalizedUserName == normalizedUserName && u.TenantId == _tenantResolver.CurrentTenantId, cancellationToken);

        if (user is null)
        {
            _logger.LogInformation("Failed login for {UserName} in tenant {Tenant}", request.UserName, _tenantResolver.CurrentTenantId);
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        if (!user.IsActive)
        {
            throw new UnauthorizedAccessException("Account is disabled");
        }

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default)
    {
        var refreshToken = await _dbContext.RefreshTokens
            .Include(x => x.User)
                .ThenInclude(u => u.UserRoles)
                    .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken && x.TenantId == _tenantResolver.CurrentTenantId, cancellationToken);

        if (refreshToken is null || !refreshToken.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid refresh token");
        }

        refreshToken.RevokedAt = DateTime.UtcNow;
        var user = refreshToken.User;
        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task LogoutAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        var token = await _dbContext.RefreshTokens.FirstOrDefaultAsync(x => x.Token == refreshToken, cancellationToken);
        if (token is null)
        {
            return;
        }

        token.RevokedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RequestOtpAsync(RequestOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            throw new ArgumentException("A valid phone number is required", nameof(request));
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == normalizedPhone && u.TenantId == _tenantResolver.CurrentTenantId, cancellationToken);

        if (user is null)
        {
            var suffixLength = Math.Min(4, normalizedPhone.Length);
            var phoneSuffix = normalizedPhone.Substring(normalizedPhone.Length - suffixLength, suffixLength);
            _logger.LogInformation("OTP requested for unknown phone number ending {PhoneSuffix} in tenant {Tenant}",
                phoneSuffix,
                _tenantResolver.CurrentTenantId);
            return;
        }

        if (!user.IsActive)
        {
            _logger.LogWarning("Inactive user {UserId} attempted OTP request", user.Id);
            return;
        }

        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            _logger.LogWarning("User {UserId} does not have a phone number configured for OTP.", user.Id);
            return;
        }

        await _otpService.RequestOtpAsync(user, request.Purpose, cancellationToken);
    }

    public async Task<AuthResponse> VerifyOtpAsync(VerifyOtpRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var normalizedPhone = NormalizePhoneNumber(request.PhoneNumber);
        if (string.IsNullOrWhiteSpace(normalizedPhone))
        {
            throw new UnauthorizedAccessException("Invalid OTP");
        }

        var user = await _dbContext.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.RefreshTokens)
            .FirstOrDefaultAsync(u => u.NormalizedPhoneNumber == normalizedPhone && u.TenantId == _tenantResolver.CurrentTenantId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            throw new UnauthorizedAccessException("Invalid OTP");
        }

        var isValid = await _otpService.ValidateOtpAsync(user, request.Code, request.Purpose, cancellationToken);
        if (!isValid)
        {
            throw new UnauthorizedAccessException("Invalid OTP");
        }

        return await IssueTokenPairAsync(user, cancellationToken);
    }

    public async Task RequestPasswordResetAsync(PasswordResetRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var normalizedEmail = NormalizeEmail(request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            throw new ArgumentException("A valid email is required", nameof(request));
        }

        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail && u.TenantId == _tenantResolver.CurrentTenantId, cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LogInformation("Password reset requested for unknown or inactive account {Email} in tenant {Tenant}",
                normalizedEmail,
                _tenantResolver.CurrentTenantId);
            return;
        }

        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddMinutes(_passwordResetOptions.TokenExpiryMinutes);

        var resetToken = new PasswordResetToken
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Token = token,
            ExpiresAt = expiresAt
        };

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var subject = "Password reset request";
        var body = $"Use the following token to reset your password: {token}. The token expires at {expiresAt:u}.";
        await _emailSender.SendAsync(user.Email, subject, body, cancellationToken);
    }

    public Task<AuthResponse> SocialLoginAsync(SocialLoginProvider provider, SocialLoginRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        _logger.LogInformation("Social login requested via {Provider} but no provider is configured.", provider);
        throw new NotImplementedException($"Social login for {provider} is not yet implemented.");
    }

    private async Task<AuthResponse> IssueTokenPairAsync(User user, CancellationToken cancellationToken)
    {
        var tokens = _tokenFactory.CreateTokenPair(user);
        user.RefreshTokens.Add(new RefreshToken
        {
            Token = tokens.RefreshToken,
            ExpiresAt = tokens.RefreshTokenExpiresAt,
            TenantId = user.TenantId
        });

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(tokens.AccessToken, tokens.RefreshToken, tokens.AccessTokenExpiresAt);
    }

    private static string NormalizePhoneNumber(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return string.Empty;
        }

        var builder = new StringBuilder(phoneNumber.Length);
        foreach (var ch in phoneNumber)
        {
            if (char.IsDigit(ch))
            {
                builder.Append(ch);
            }
        }

        return builder.ToString();
    }

    private static string NormalizeEmail(string email) =>
        string.IsNullOrWhiteSpace(email) ? string.Empty : email.Trim().ToUpperInvariant();

    private static string GenerateSecureToken(int size = 32)
    {
        var buffer = new byte[size];
        RandomNumberGenerator.Fill(buffer);
        return Convert.ToBase64String(buffer);
    }
}
