using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Infrastructure.Theming;

public class ThemePreviewService : IThemePreviewService
{
    private readonly ThemePreviewOptions _options;
    private readonly SigningCredentials _signingCredentials;
    private readonly TokenValidationParameters _validationParameters;

    public ThemePreviewService(IOptions<ThemePreviewOptions> options)
    {
        _options = options.Value;
        var keyBytes = Encoding.UTF8.GetBytes(_options.SigningKey);
        var securityKey = new SymmetricSecurityKey(keyBytes);
        _signingCredentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = securityKey,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30)
        };
    }

    public string GeneratePreviewToken(Guid themeId, int expiryMinutes)
    {
        var expires = DateTime.UtcNow.AddMinutes(expiryMinutes <= 0 ? _options.DefaultExpiryMinutes : expiryMinutes);
        var token = new JwtSecurityToken(
            claims: new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, themeId.ToString()),
                new Claim("themeId", themeId.ToString())
            },
            expires: expires,
            signingCredentials: _signingCredentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public bool TryValidateToken(string token, out Guid themeId, out DateTime expiresAt)
    {
        themeId = Guid.Empty;
        expiresAt = DateTime.MinValue;

        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        var handler = new JwtSecurityTokenHandler();
        try
        {
            var principal = handler.ValidateToken(token, _validationParameters, out var validatedToken);
            var sub = principal.FindFirstValue("themeId") ?? principal.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (!Guid.TryParse(sub, out themeId))
            {
                return false;
            }

            if (validatedToken is JwtSecurityToken jwt && jwt.ValidTo != default)
            {
                expiresAt = jwt.ValidTo;
            }

            return expiresAt > DateTime.UtcNow;
        }
        catch
        {
            return false;
        }
    }

    public Uri BuildPreviewUrl(Guid themeId, string token)
    {
        var host = _options.PreviewSubdomain.TrimEnd('/');
        var scheme = host.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? string.Empty : "https://";
        var baseUrl = $"{(string.IsNullOrEmpty(scheme) ? string.Empty : scheme)}{host}";
        return new Uri($"{baseUrl}/theme-preview/{themeId}?token={Uri.EscapeDataString(token)}");
    }
}
