namespace MultiTenantEcommerce.Domain.Entities;

public class ThemeVariable
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantThemeId { get; set; }
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;

    public TenantTheme? TenantTheme { get; set; }
}
