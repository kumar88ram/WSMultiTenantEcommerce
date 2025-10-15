using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class TenantThemeConfiguration : IEntityTypeConfiguration<TenantTheme>
{
    public void Configure(EntityTypeBuilder<TenantTheme> builder)
    {
        builder.ToTable("TenantThemes");
        builder.HasKey(tt => tt.Id);

        builder.HasIndex(tt => new { tt.TenantId, tt.IsActive })
            .HasFilter("[IsActive] = 1");

        builder.Property(tt => tt.ActivatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasMany(tt => tt.Variables)
            .WithOne(v => v.TenantTheme)
            .HasForeignKey(v => v.TenantThemeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tt => tt.Sections)
            .WithOne()
            .HasForeignKey(s => s.TenantThemeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
