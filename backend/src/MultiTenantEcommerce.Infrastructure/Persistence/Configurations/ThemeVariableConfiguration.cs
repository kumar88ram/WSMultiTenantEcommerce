using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ThemeVariableConfiguration : IEntityTypeConfiguration<ThemeVariable>
{
    public void Configure(EntityTypeBuilder<ThemeVariable> builder)
    {
        builder.ToTable("ThemeVariables");
        builder.HasKey(v => v.Id);

        builder.Property(v => v.Key)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(v => v.Value)
            .HasColumnType("nvarchar(max)")
            .IsRequired();
    }
}
