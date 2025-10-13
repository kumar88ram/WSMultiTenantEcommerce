using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class StoreSettingConfiguration : IEntityTypeConfiguration<StoreSetting>
{
    public void Configure(EntityTypeBuilder<StoreSetting> builder)
    {
        builder.ToTable("StoreSettings");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(s => s.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(s => s.Timezone)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(s => new { s.TenantId }).IsUnique();
    }
}
