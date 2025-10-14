using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class AnalyticsEventConfiguration : IEntityTypeConfiguration<AnalyticsEvent>
{
    public void Configure(EntityTypeBuilder<AnalyticsEvent> builder)
    {
        builder.ToTable("AnalyticsEvents");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(e => e.Metadata)
            .HasMaxLength(2048);

        builder.Property(e => e.Amount)
            .HasColumnType("decimal(18,2)");

        builder.HasIndex(e => new { e.TenantId, e.OccurredAt });
        builder.HasIndex(e => new { e.TenantId, e.EventType, e.OccurredAt });
    }
}
