using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class TenantThemeSectionConfiguration : IEntityTypeConfiguration<TenantThemeSection>
{
    public void Configure(EntityTypeBuilder<TenantThemeSection> builder)
    {
        builder.ToTable("TenantThemeSections");
        builder.HasIndex(x => x.TenantThemeId);
        builder.HasIndex(x => x.TenantId);
    }
}
