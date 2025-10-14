using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    private readonly ITenantResolver? _tenantResolver;
    private readonly MultiTenancyOptions _multiTenancyOptions;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ITenantResolver? tenantResolver,
        IOptions<MultiTenancyOptions>? multiTenancyOptions) : base(options)
    {
        _tenantResolver = tenantResolver;
        _multiTenancyOptions = multiTenancyOptions?.Value ?? new MultiTenancyOptions();
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<StoreSetting> StoreSettings => Set<StoreSetting>();
    public DbSet<Theme> Themes => Set<Theme>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductCategory> ProductCategories => Set<ProductCategory>();
    public DbSet<ProductAttribute> ProductAttributes => Set<ProductAttribute>();
    public DbSet<AttributeValue> AttributeValues => Set<AttributeValue>();
    public DbSet<ProductVariant> ProductVariants => Set<ProductVariant>();
    public DbSet<ProductVariantAttributeValue> ProductVariantAttributeValues => Set<ProductVariantAttributeValue>();
    public DbSet<ProductImage> ProductImages => Set<ProductImage>();
    public DbSet<Inventory> Inventories => Set<Inventory>();
    public DbSet<MenuDefinition> MenuDefinitions => Set<MenuDefinition>();
    public DbSet<FormDefinition> FormDefinitions => Set<FormDefinition>();
    public DbSet<FormField> FormFields => Set<FormField>();
    public DbSet<WidgetDefinition> WidgetDefinitions => Set<WidgetDefinition>();
    public DbSet<Cart> Carts => Set<Cart>();
    public DbSet<CartItem> CartItems => Set<CartItem>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<PaymentTransaction> PaymentTransactions => Set<PaymentTransaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Ignore<Tenant>();

        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId, ur.TenantId });
        modelBuilder.Entity<User>().HasIndex(u => new { u.NormalizedUserName, u.TenantId }).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => new { r.NormalizedName, r.TenantId }).IsUnique();

        if (_multiTenancyOptions.UseSharedDatabase && _tenantResolver is not null)
        {
            modelBuilder.Entity<User>().HasQueryFilter(u => u.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<Role>().HasQueryFilter(r => r.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<UserRole>().HasQueryFilter(ur => ur.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<RefreshToken>().HasQueryFilter(rt => rt.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<StoreSetting>().HasQueryFilter(s => s.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<Theme>().HasQueryFilter(t => t.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<Category>().HasQueryFilter(c => c.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<Product>().HasQueryFilter(p => p.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<ProductAttribute>().HasQueryFilter(pa => pa.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<AttributeValue>().HasQueryFilter(av => av.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<ProductVariant>().HasQueryFilter(pv => pv.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<ProductVariantAttributeValue>().HasQueryFilter(pvav => pvav.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<ProductImage>().HasQueryFilter(pi => pi.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<Inventory>().HasQueryFilter(i => i.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<MenuDefinition>().HasQueryFilter(m => m.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<FormDefinition>().HasQueryFilter(f => f.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<FormField>().HasQueryFilter(f => f.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<WidgetDefinition>().HasQueryFilter(w => w.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<ProductCategory>().HasQueryFilter(pc => pc.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<Cart>().HasQueryFilter(c => c.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<CartItem>().HasQueryFilter(ci => ci.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<Order>().HasQueryFilter(o => o.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<OrderItem>().HasQueryFilter(oi => oi.TenantId == _tenantResolver.CurrentTenantId);
            modelBuilder.Entity<PaymentTransaction>().HasQueryFilter(pt => pt.TenantId == _tenantResolver.CurrentTenantId);
        }
    }
}
