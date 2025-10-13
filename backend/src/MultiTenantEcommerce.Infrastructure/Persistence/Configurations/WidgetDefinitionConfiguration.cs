using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class WidgetDefinitionConfiguration : IEntityTypeConfiguration<WidgetDefinition>
{
    public void Configure(EntityTypeBuilder<WidgetDefinition> builder)
    {
        builder.ToTable("WidgetDefinitions");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(w => w.Type)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(w => new { w.TenantId, w.Name }).IsUnique();
    }
}
