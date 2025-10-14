namespace MultiTenantEcommerce.Application.Models;

public class PasswordResetOptions
{
    public int TokenExpiryMinutes { get; set; } = 60;
}
