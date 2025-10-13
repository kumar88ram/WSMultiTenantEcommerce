using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class FormDefinitionConfiguration :
    IEntityTypeConfiguration<FormDefinition>,
    IEntityTypeConfiguration<FormField>
{
    public void Configure(EntityTypeBuilder<FormDefinition> builder)
    {
        builder.ToTable("FormDefinitions");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.HasMany(f => f.Fields)
            .WithOne(field => field.FormDefinition)
            .HasForeignKey(field => field.FormDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<FormField> builder)
    {
        builder.ToTable("FormFields");
        builder.HasKey(f => f.Id);

        builder.Property(f => f.Label)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(f => f.Name)
            .HasMaxLength(150)
            .IsRequired();

        builder.Property(f => f.Type)
            .HasMaxLength(50)
            .IsRequired();
    }
}
