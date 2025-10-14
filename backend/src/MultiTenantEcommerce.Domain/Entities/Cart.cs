using System.Collections.ObjectModel;

namespace MultiTenantEcommerce.Domain.Entities;

public class Cart : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? GuestToken { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }

    public ICollection<CartItem> Items { get; set; } = new Collection<CartItem>();
}
