namespace MultiTenantEcommerce.Domain.Entities;

public class Theme
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PreviewImageUrl { get; set; }
    public string ZipFilePath { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public ICollection<ThemeSection> Sections { get; set; } = new List<ThemeSection>();
    public ICollection<TenantTheme> TenantThemes { get; set; } = new List<TenantTheme>();
}
