using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class SubscriptionPlanConfiguration : IEntityTypeConfiguration<SubscriptionPlan>
{
    public void Configure(EntityTypeBuilder<SubscriptionPlan> builder)
    {
        builder.ToTable("SubscriptionPlans");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Description).HasMaxLength(1024);
        builder.Property(x => x.Currency).HasMaxLength(3);
        builder.Property(x => x.BillingPeriod).HasMaxLength(32);
        builder.Property(x => x.Price).HasColumnType("decimal(18,2)");

        builder.OwnsOne(x => x.BillingMetadata, navigationBuilder =>
        {
            navigationBuilder.Property(m => m.ExternalPlanId).HasMaxLength(128);
        });
    }
}
