namespace MultiTenantEcommerce.Application.Models;

public class ThemePreviewOptions
{
    public string PreviewSubdomain { get; set; } = "preview";
    public string SigningKey { get; set; } = "ChangeMeThemePreviewSigningKey";
    public int DefaultExpiryMinutes { get; set; } = 10;
}
