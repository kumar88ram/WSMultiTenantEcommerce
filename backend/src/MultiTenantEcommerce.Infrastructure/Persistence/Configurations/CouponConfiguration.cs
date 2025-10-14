using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class CouponConfiguration : IEntityTypeConfiguration<Coupon>
{
    public void Configure(EntityTypeBuilder<Coupon> builder)
    {
        builder.ToTable("Coupons");

        builder.HasIndex(c => new { c.TenantId, c.Code }).IsUnique();
        builder.Property(c => c.Code)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(c => c.Value)
            .HasPrecision(18, 2);
        builder.Property(c => c.MinimumOrderAmount)
            .HasPrecision(18, 2);

    }
}
