using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class OrderConfiguration :
    IEntityTypeConfiguration<Order>,
    IEntityTypeConfiguration<OrderItem>,
    IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.OrderNumber)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(o => o.Status)
            .HasConversion<int>();

        builder.Property(o => o.Subtotal).HasPrecision(18, 2);
        builder.Property(o => o.DiscountTotal).HasPrecision(18, 2);
        builder.Property(o => o.TaxTotal).HasPrecision(18, 2);
        builder.Property(o => o.ShippingTotal).HasPrecision(18, 2);
        builder.Property(o => o.GrandTotal).HasPrecision(18, 2);

        builder.Property(o => o.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(o => o.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(o => o.ShippingAddress)
            .HasMaxLength(1024);

        builder.Property(o => o.BillingAddress)
            .HasMaxLength(1024);

        builder.Property(o => o.CouponCode)
            .HasMaxLength(100);

        builder.HasIndex(o => new { o.TenantId, o.OrderNumber }).IsUnique();

        builder.HasOne(o => o.PromotionCampaign)
            .WithMany()
            .HasForeignKey(o => o.PromotionCampaignId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(o => o.Coupon)
            .WithMany(c => c.Orders)
            .HasForeignKey(o => o.CouponId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(o => o.Payments)
            .WithOne(p => p.Order)
            .HasForeignKey(p => p.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.HasKey(oi => oi.Id);

        builder.Property(oi => oi.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(oi => oi.Sku)
            .HasMaxLength(100);

        builder.Property(oi => oi.UnitPrice).HasPrecision(18, 2);
        builder.Property(oi => oi.LineTotal).HasPrecision(18, 2);
    }

    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("PaymentTransactions");
        builder.HasKey(pt => pt.Id);

        builder.Property(pt => pt.Provider)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pt => pt.ProviderReference)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(pt => pt.Amount).HasPrecision(18, 2);
        builder.Property(pt => pt.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(pt => pt.Status)
            .HasConversion<int>();

        builder.HasIndex(pt => new { pt.Provider, pt.ProviderReference }).IsUnique();
    }
}
