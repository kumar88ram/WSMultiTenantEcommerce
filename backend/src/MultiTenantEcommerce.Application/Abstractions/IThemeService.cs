using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IThemeService
{
    Task<Theme> UploadThemeAsync(ThemeUploadContext context, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Theme>> GetThemesAsync(CancellationToken cancellationToken = default);
    Task<TenantTheme?> ActivateThemeAsync(Guid themeId, Guid tenantId, CancellationToken cancellationToken = default);
    Task DeactivateThemeAsync(Guid themeId, Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantTheme?> GetActiveThemeAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ThemeSection>> UpsertSectionsAsync(Guid themeId, IEnumerable<ThemeSectionDefinition> sections, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ThemeVariable>> UpdateVariablesAsync(Guid tenantThemeId, IDictionary<string, string> variables, CancellationToken cancellationToken = default);
}
