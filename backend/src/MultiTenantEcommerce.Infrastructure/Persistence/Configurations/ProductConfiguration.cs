using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ProductConfiguration :
    IEntityTypeConfiguration<Product>,
    IEntityTypeConfiguration<ProductCategory>,
    IEntityTypeConfiguration<ProductAttribute>,
    IEntityTypeConfiguration<AttributeValue>,
    IEntityTypeConfiguration<ProductVariant>,
    IEntityTypeConfiguration<ProductVariantAttributeValue>,
    IEntityTypeConfiguration<Inventory>,
    IEntityTypeConfiguration<ProductImage>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Slug)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        builder.Property(p => p.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.HasIndex(p => new { p.TenantId, p.Slug }).IsUnique();

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Attributes)
            .WithOne(a => a.Product)
            .HasForeignKey(a => a.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Variants)
            .WithOne(v => v.Product)
            .HasForeignKey(v => v.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Inventory)
            .WithOne(i => i.Product)
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ProductCategory> builder)
    {
        builder.ToTable("ProductCategories");
        builder.HasKey(pc => new { pc.ProductId, pc.CategoryId });

        builder.HasOne(pc => pc.Product)
            .WithMany(p => p.ProductCategories)
            .HasForeignKey(pc => pc.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Category)
            .WithMany(c => c.ProductCategories)
            .HasForeignKey(pc => pc.CategoryId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("ProductAttributes");
        builder.HasKey(pa => pa.Id);

        builder.Property(pa => pa.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(pa => pa.DisplayName)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasIndex(pa => new { pa.TenantId, pa.ProductId, pa.Name }).IsUnique();

        builder.HasMany(pa => pa.Values)
            .WithOne(v => v.ProductAttribute)
            .HasForeignKey(v => v.ProductAttributeId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<AttributeValue> builder)
    {
        builder.ToTable("AttributeValues");
        builder.HasKey(av => av.Id);

        builder.Property(av => av.Value)
            .HasMaxLength(400)
            .IsRequired();

        builder.Property(av => av.SortOrder)
            .HasDefaultValue(0);

        builder.HasIndex(av => new { av.TenantId, av.ProductAttributeId, av.Value }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(pv => pv.Sku)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(pv => pv.Price)
            .HasPrecision(18, 2);

        builder.Property(pv => pv.CompareAtPrice)
            .HasPrecision(18, 2);

        builder.HasIndex(pv => new { pv.TenantId, pv.Sku }).IsUnique();

        builder.HasMany(pv => pv.AttributeValues)
            .WithOne(v => v.ProductVariant)
            .HasForeignKey(v => v.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pv => pv.Inventory)
            .WithOne(i => i.ProductVariant)
            .HasForeignKey<Inventory>(i => i.ProductVariantId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ProductVariantAttributeValue> builder)
    {
        builder.ToTable("ProductVariantAttributeValues");
        builder.HasKey(pvav => pvav.Id);

        builder.HasIndex(pvav => new { pvav.TenantId, pvav.ProductVariantId, pvav.AttributeValueId }).IsUnique();

        builder.HasOne(pvav => pvav.AttributeValue)
            .WithMany(av => av.VariantValues)
            .HasForeignKey(pvav => pvav.AttributeValueId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<Inventory> builder)
    {
        builder.ToTable("Inventories");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.QuantityOnHand)
            .HasDefaultValue(0);

        builder.Property(i => i.ReservedQuantity)
            .HasDefaultValue(0);

        builder.HasIndex(i => new { i.TenantId, i.ProductId, i.ProductVariantId }).IsUnique();
    }

    public void Configure(EntityTypeBuilder<ProductImage> builder)
    {
        builder.ToTable("ProductImages");
        builder.HasKey(pi => pi.Id);

        builder.Property(pi => pi.Url)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(pi => pi.AltText)
            .HasMaxLength(200);
    }
}
