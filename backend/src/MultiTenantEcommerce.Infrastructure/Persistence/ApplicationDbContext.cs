using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

        modelBuilder.Entity<UserRole>().HasKey(ur => new { ur.UserId, ur.RoleId, ur.TenantId });
        modelBuilder.Entity<User>().HasIndex(u => new { u.NormalizedUserName, u.TenantId }).IsUnique();
        modelBuilder.Entity<Role>().HasIndex(r => new { r.NormalizedName, r.TenantId }).IsUnique();
        modelBuilder.Entity<Tenant>().HasIndex(t => t.Identifier).IsUnique();
    }
}
