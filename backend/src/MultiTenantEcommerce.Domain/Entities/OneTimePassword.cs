using System;

namespace MultiTenantEcommerce.Domain.Entities;

public class OneTimePassword : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = default!;
    public string Code { get; set; } = string.Empty;
    public string Purpose { get; set; } = "login";
    public string Destination { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public int AttemptCount { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public bool IsVerified => VerifiedAt.HasValue;
}
