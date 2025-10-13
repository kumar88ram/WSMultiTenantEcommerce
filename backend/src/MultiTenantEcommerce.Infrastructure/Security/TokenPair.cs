using System.Security.Claims;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Security;

public record TokenPair(string AccessToken, DateTime AccessTokenExpiresAt, string RefreshToken, DateTime RefreshTokenExpiresAt, IEnumerable<Claim> Claims)
{
    public static TokenPair Empty => new(string.Empty, DateTime.MinValue, string.Empty, DateTime.MinValue, Array.Empty<Claim>());
}
