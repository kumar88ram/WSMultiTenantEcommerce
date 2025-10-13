using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(t => t.Subdomain)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(t => t.CustomDomain)
            .HasMaxLength(256);

        builder.Property(t => t.DbConnectionString)
            .HasMaxLength(2048)
            .IsRequired();

        builder.Property(t => t.PlanId)
            .IsRequired();

        builder.Property(t => t.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        builder.HasIndex(t => t.Subdomain).IsUnique();
        builder.HasIndex(t => t.CustomDomain).IsUnique().HasFilter("[CustomDomain] IS NOT NULL");

        builder.HasOne<SubscriptionPlan>()
            .WithMany()
            .HasForeignKey(t => t.PlanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
