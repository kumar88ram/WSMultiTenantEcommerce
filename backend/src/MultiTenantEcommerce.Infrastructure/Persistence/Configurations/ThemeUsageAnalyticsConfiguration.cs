using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ThemeUsageAnalyticsConfiguration : IEntityTypeConfiguration<ThemeUsageAnalytics>
{
    public void Configure(EntityTypeBuilder<ThemeUsageAnalytics> builder)
    {
        builder.ToTable("ThemeUsageAnalytics");
        builder.HasIndex(x => new { x.ThemeId, x.TenantId, x.IsActive });
    }
}
