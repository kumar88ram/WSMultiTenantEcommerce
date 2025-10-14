using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class TaxRuleConfiguration : IEntityTypeConfiguration<TaxRule>
{
    public void Configure(EntityTypeBuilder<TaxRule> builder)
    {
        builder.ToTable("TaxRules");
        builder.HasKey(rule => rule.Id);

        builder.Property(rule => rule.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(rule => rule.StateCode)
            .HasMaxLength(10);

        builder.Property(rule => rule.CalculationType)
            .HasConversion<int>();

        builder.Property(rule => rule.Rate)
            .HasPrecision(18, 4)
            .IsRequired();

        builder.HasIndex(rule => new { rule.TenantId, rule.CountryCode, rule.StateCode });
    }
}
