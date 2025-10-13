using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    public AuthService(
        ApplicationDbContext dbContext,
        ITokenFactory tokenFactory,
        IPasswordHasher passwordHasher,
        ITenantResolver tenantResolver,
        ILogger<AuthService> logger)
    {
        _dbContext = dbContext;
        _tokenFactory = tokenFactory;
        _passwordHasher = passwordHasher;
        _tenantResolver = tenantResolver;
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

        if (!_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            throw new UnauthorizedAccessException("Invalid credentials");
        }

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
}
