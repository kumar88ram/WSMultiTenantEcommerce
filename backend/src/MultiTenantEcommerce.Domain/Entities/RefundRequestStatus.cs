namespace MultiTenantEcommerce.Domain.Entities;

public enum RefundRequestStatus
{
    Pending = 0,
    Approved = 1,
    Denied = 2,
    Refunded = 3,
    Failed = 4
}
