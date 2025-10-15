using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ThemeAuditLogConfiguration : IEntityTypeConfiguration<ThemeAuditLog>
{
    public void Configure(EntityTypeBuilder<ThemeAuditLog> builder)
    {
        builder.ToTable("ThemeAuditLogs");
        builder.Property(l => l.Action).HasMaxLength(128);
        builder.HasIndex(l => l.ThemeId);
        builder.HasIndex(l => l.Timestamp);
    }
}
