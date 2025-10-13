using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class MenuDefinitionConfiguration : IEntityTypeConfiguration<MenuDefinition>
{
    public void Configure(EntityTypeBuilder<MenuDefinition> builder)
    {
        builder.ToTable("MenuDefinitions");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(m => new { m.TenantId, m.Name }).IsUnique();
    }
}
