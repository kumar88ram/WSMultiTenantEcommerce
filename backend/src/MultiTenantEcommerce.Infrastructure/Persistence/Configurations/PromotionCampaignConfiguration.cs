using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class PromotionCampaignConfiguration : IEntityTypeConfiguration<PromotionCampaign>
{
    public void Configure(EntityTypeBuilder<PromotionCampaign> builder)
    {
        builder.ToTable("PromotionCampaigns");
        builder.Property(c => c.Name)
            .HasMaxLength(128)
            .IsRequired();
        builder.Property(c => c.Value)
            .HasPrecision(18, 2);
        builder.Property(c => c.MinimumOrderAmount)
            .HasPrecision(18, 2);
        builder.HasIndex(c => new { c.TenantId, c.IsActive, c.StartsAt });
    }
}
