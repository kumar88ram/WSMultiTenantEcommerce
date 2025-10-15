namespace MultiTenantEcommerce.Application.Abstractions;

public interface IThemePreviewService
{
    string GeneratePreviewToken(Guid themeId, int expiryMinutes);
    bool TryValidateToken(string token, out Guid themeId, out DateTime expiresAt);
    Uri BuildPreviewUrl(Guid themeId, string token);
}
