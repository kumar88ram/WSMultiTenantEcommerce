using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ProductConfiguration :
    IEntityTypeConfiguration<Product>,
    IEntityTypeConfiguration<ProductCategory>,
    IEntityTypeConfiguration<ProductAttribute>,
    IEntityTypeConfiguration<ProductVariant>,
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

        builder.Property(pa => pa.Value)
            .HasMaxLength(400)
            .IsRequired();
    }

    public void Configure(EntityTypeBuilder<ProductVariant> builder)
    {
        builder.ToTable("ProductVariants");
        builder.HasKey(pv => pv.Id);

        builder.Property(pv => pv.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(pv => pv.Sku)
            .HasMaxLength(100);

        builder.Property(pv => pv.Price)
            .HasPrecision(18, 2);
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
