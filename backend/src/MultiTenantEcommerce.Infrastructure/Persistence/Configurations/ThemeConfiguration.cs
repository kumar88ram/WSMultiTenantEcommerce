using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ThemeConfiguration : IEntityTypeConfiguration<Theme>
{
    public void Configure(EntityTypeBuilder<Theme> builder)
    {
        builder.ToTable("Themes");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.DisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(t => new { t.TenantId, t.Name }).IsUnique();
    }
}
