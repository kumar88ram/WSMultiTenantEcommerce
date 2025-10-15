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
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Code)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.Version)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.PreviewImageUrl)
            .HasMaxLength(1024);

        builder.Property(t => t.ZipFilePath)
            .HasMaxLength(1024)
            .IsRequired();

        builder.HasIndex(t => new { t.Code, t.Version }).IsUnique();

        builder.HasMany(t => t.Sections)
            .WithOne(s => s.Theme)
            .HasForeignKey(s => s.ThemeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(t => t.TenantThemes)
            .WithOne(tt => tt.Theme)
            .HasForeignKey(tt => tt.ThemeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
