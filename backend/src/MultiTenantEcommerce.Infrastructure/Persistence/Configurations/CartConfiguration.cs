using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class CartConfiguration :
    IEntityTypeConfiguration<Cart>,
    IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.HasKey(c => c.Id);

        builder.HasIndex(c => new { c.TenantId, c.UserId })
            .IsUnique()
            .HasFilter("[UserId] IS NOT NULL");

        builder.HasIndex(c => new { c.TenantId, c.GuestToken })
            .IsUnique()
            .HasFilter("[GuestToken] IS NOT NULL");

        builder.Property(c => c.GuestToken)
            .HasMaxLength(100);

        builder.Property(c => c.IsActive)
            .HasDefaultValue(true);

        builder.HasMany(c => c.Items)
            .WithOne(i => i.Cart)
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.HasKey(ci => ci.Id);

        builder.Property(ci => ci.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(ci => ci.Sku)
            .HasMaxLength(100);

        builder.Property(ci => ci.UnitPrice)
            .HasPrecision(18, 2);
    }
}
