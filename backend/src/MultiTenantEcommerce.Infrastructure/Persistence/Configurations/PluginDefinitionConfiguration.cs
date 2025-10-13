using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class PluginDefinitionConfiguration : IEntityTypeConfiguration<PluginDefinition>
{
    public void Configure(EntityTypeBuilder<PluginDefinition> builder)
    {
        builder.ToTable("Plugins");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).IsRequired().HasMaxLength(128);
        builder.Property(x => x.SystemKey).IsRequired().HasMaxLength(128);
        builder.Property(x => x.Version).HasMaxLength(32);
        builder.Property(x => x.Description).HasMaxLength(1024);

        builder.HasIndex(x => x.SystemKey).IsUnique();
    }
}

public class TenantPluginConfiguration : IEntityTypeConfiguration<TenantPlugin>
{
    public void Configure(EntityTypeBuilder<TenantPlugin> builder)
    {
        builder.ToTable("TenantPlugins");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.ConfigurationJson).HasMaxLength(2048);

        builder.HasIndex(x => new { x.TenantId, x.PluginId }).IsUnique();

        builder.HasOne(x => x.Plugin)
            .WithMany(x => x.TenantPlugins)
            .HasForeignKey(x => x.PluginId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Tenant>()
            .WithMany()
            .HasForeignKey(x => x.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
