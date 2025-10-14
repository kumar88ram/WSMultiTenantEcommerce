using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class RefundRequestConfiguration :
    IEntityTypeConfiguration<RefundRequest>,
    IEntityTypeConfiguration<RefundRequestItem>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.ToTable("RefundRequests");
        builder.HasKey(request => request.Id);

        builder.Property(request => request.Reason)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(request => request.Status)
            .HasConversion<int>();

        builder.Property(request => request.RequestedAmount)
            .HasPrecision(18, 2);

        builder.Property(request => request.ApprovedAmount)
            .HasPrecision(18, 2);

        builder.Property(request => request.DecisionNotes)
            .HasMaxLength(500);

        builder.HasOne(request => request.PaymentTransaction)
            .WithOne()
            .HasForeignKey<RefundRequest>(request => request.PaymentTransactionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(request => request.Items)
            .WithOne(item => item.RefundRequest)
            .HasForeignKey(item => item.RefundRequestId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<RefundRequestItem> builder)
    {
        builder.ToTable("RefundRequestItems");
        builder.HasKey(item => item.Id);

        builder.Property(item => item.Quantity);

        builder.Property(item => item.LineTotal)
            .HasPrecision(18, 2);
    }
}
