using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class DailyAnalyticsSummaryConfiguration : IEntityTypeConfiguration<DailyAnalyticsSummary>
{
    public void Configure(EntityTypeBuilder<DailyAnalyticsSummary> builder)
    {
        builder.ToTable("DailyAnalyticsSummaries");
        builder.HasKey(summary => summary.Id);

        builder.Property(summary => summary.Date)
            .HasColumnType("date")
            .HasConversion(
                value => value.ToDateTime(TimeOnly.MinValue),
                value => DateOnly.FromDateTime(value));

        builder.Property(summary => summary.SalesAmount)
            .HasColumnType("decimal(18,2)");

        builder.Property(summary => summary.ConversionRate)
            .HasColumnType("decimal(18,4)");

        builder.HasIndex(summary => new { summary.TenantId, summary.Date })
            .IsUnique();
    }
}
