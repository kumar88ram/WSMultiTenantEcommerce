namespace MultiTenantEcommerce.Domain.Entities;

public class TenantTheme
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid TenantId { get; set; }
    public Guid ThemeId { get; set; }
    public DateTime ActivatedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; }

    public Theme? Theme { get; set; }
    public ICollection<ThemeVariable> Variables { get; set; } = new List<ThemeVariable>();
    public ICollection<TenantThemeSection> Sections { get; set; } = new List<TenantThemeSection>();
}
