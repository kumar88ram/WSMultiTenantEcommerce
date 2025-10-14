using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Configurations;

public class SupportTicketConfiguration : IEntityTypeConfiguration<SupportTicket>, IEntityTypeConfiguration<SupportTicketMessage>, IEntityTypeConfiguration<SupportTicketAttachment>
{
    public void Configure(EntityTypeBuilder<SupportTicket> builder)
    {
        builder.Property(t => t.Subject)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(t => t.Description)
            .HasMaxLength(2048);

        builder.Property(t => t.CustomerName)
            .HasMaxLength(256);

        builder.Property(t => t.CustomerEmail)
            .HasMaxLength(256);

        builder.HasMany(t => t.Messages)
            .WithOne(m => m.SupportTicket)
            .HasForeignKey(m => m.SupportTicketId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<SupportTicketMessage> builder)
    {
        builder.Property(m => m.Body)
            .IsRequired()
            .HasMaxLength(4000);

        builder.Property(m => m.AuthorName)
            .HasMaxLength(256);

        builder.Property(m => m.AuthorEmail)
            .HasMaxLength(256);

        builder.HasMany(m => m.Attachments)
            .WithOne(a => a.SupportTicketMessage)
            .HasForeignKey(a => a.SupportTicketMessageId)
            .OnDelete(DeleteBehavior.Cascade);
    }

    public void Configure(EntityTypeBuilder<SupportTicketAttachment> builder)
    {
        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.Url)
            .IsRequired()
            .HasMaxLength(1024);
    }
}
