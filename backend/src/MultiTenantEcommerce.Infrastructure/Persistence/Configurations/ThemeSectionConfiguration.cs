using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ThemeSectionConfiguration : IEntityTypeConfiguration<ThemeSection>
{
    public void Configure(EntityTypeBuilder<ThemeSection> builder)
    {
        builder.ToTable("ThemeSections");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.SectionName)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.JsonConfig)
            .HasColumnType("nvarchar(max)")
            .IsRequired();

        builder.Property(s => s.SortOrder)
            .HasDefaultValue(0);
    }
}
