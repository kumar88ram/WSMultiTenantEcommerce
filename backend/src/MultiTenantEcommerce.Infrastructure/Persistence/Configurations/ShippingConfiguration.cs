using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class ShippingConfiguration :
    IEntityTypeConfiguration<ShippingZone>,
    IEntityTypeConfiguration<ShippingZoneRegion>,
    IEntityTypeConfiguration<ShippingMethod>,
    IEntityTypeConfiguration<ShippingRateTableEntry>
{
    public void Configure(EntityTypeBuilder<ShippingZone> builder)
    {
        builder.ToTable("ShippingZones");
        builder.HasKey(zone => zone.Id);

        builder.Property(zone => zone.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.HasIndex(zone => new { zone.TenantId, zone.Name }).IsUnique();

        builder.HasMany(zone => zone.Regions)
            .WithOne(region => region.ShippingZone)
            .HasForeignKey(region => region.ShippingZoneId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(zone => zone.Methods)
            .WithOne(method => method.ShippingZone)
            .HasForeignKey(method => method.ShippingZoneId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ShippingZoneRegion> builder)
    {
        builder.ToTable("ShippingZoneRegions");
        builder.HasKey(region => region.Id);

        builder.Property(region => region.CountryCode)
            .HasMaxLength(2)
            .IsRequired();

        builder.Property(region => region.StateCode)
            .HasMaxLength(10);
    }

    public void Configure(EntityTypeBuilder<ShippingMethod> builder)
    {
        builder.ToTable("ShippingMethods");
        builder.HasKey(method => method.Id);

        builder.Property(method => method.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(method => method.Description)
            .HasMaxLength(512);

        builder.Property(method => method.Currency)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(method => method.MethodType)
            .HasConversion<int>();

        builder.Property(method => method.RateConditionType)
            .HasConversion<int>();

        builder.Property(method => method.FlatRate)
            .HasPrecision(18, 2);

        builder.Property(method => method.MinimumOrderTotal)
            .HasPrecision(18, 2);

        builder.Property(method => method.MaximumOrderTotal)
            .HasPrecision(18, 2);

        builder.Property(method => method.CarrierKey)
            .HasMaxLength(100);

        builder.Property(method => method.CarrierServiceLevel)
            .HasMaxLength(100);

        builder.Property(method => method.IntegrationSettingsJson)
            .HasMaxLength(2000);

        builder.Property(method => method.EstimatedTransitMinDays);
        builder.Property(method => method.EstimatedTransitMaxDays);

        builder.HasMany(method => method.RateTable)
            .WithOne(entry => entry.ShippingMethod)
            .HasForeignKey(entry => entry.ShippingMethodId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<ShippingRateTableEntry> builder)
    {
        builder.ToTable("ShippingRateTableEntries");
        builder.HasKey(entry => entry.Id);

        builder.Property(entry => entry.MinValue)
            .HasPrecision(18, 2);

        builder.Property(entry => entry.MaxValue)
            .HasPrecision(18, 2);

        builder.Property(entry => entry.Rate)
            .HasPrecision(18, 2);
    }
}
