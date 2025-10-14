using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(u => u.UserName).HasMaxLength(256);
        builder.Property(u => u.NormalizedUserName).HasMaxLength(256);
        builder.Property(u => u.Email).HasMaxLength(256);
        builder.Property(u => u.NormalizedEmail).HasMaxLength(256);
        builder.Property(u => u.PhoneNumber).HasMaxLength(32);
        builder.Property(u => u.NormalizedPhoneNumber).HasMaxLength(32);

        builder.HasIndex(u => new { u.NormalizedPhoneNumber, u.TenantId }).HasFilter("[NormalizedPhoneNumber] IS NOT NULL").IsUnique();
    }
}
