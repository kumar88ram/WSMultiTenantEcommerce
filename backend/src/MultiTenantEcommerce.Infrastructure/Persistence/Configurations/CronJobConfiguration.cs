using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class CronJobConfiguration : IEntityTypeConfiguration<CronJob>
{
    public void Configure(EntityTypeBuilder<CronJob> builder)
    {
        builder.ToTable("CronJobs");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.ScheduleExpression).IsRequired().HasMaxLength(64);
        builder.Property(x => x.Handler).IsRequired().HasMaxLength(256);
    }
}
