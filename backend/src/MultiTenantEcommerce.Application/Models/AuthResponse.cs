namespace MultiTenantEcommerce.Application.Models;

public record AuthResponse(string AccessToken, string RefreshToken, DateTime ExpiresAt);
