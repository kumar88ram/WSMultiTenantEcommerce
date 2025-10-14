using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class OneTimePasswordConfiguration : IEntityTypeConfiguration<OneTimePassword>
{
    public void Configure(EntityTypeBuilder<OneTimePassword> builder)
    {
        builder.Property(o => o.Code).HasMaxLength(32);
        builder.Property(o => o.Purpose).HasMaxLength(64);
        builder.Property(o => o.Destination).HasMaxLength(64);

        builder.HasIndex(o => new { o.UserId, o.Purpose, o.CreatedAt });
    }
}
