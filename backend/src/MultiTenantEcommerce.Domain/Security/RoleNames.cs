namespace MultiTenantEcommerce.Domain.Security;

public static class RoleNames
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantAdmin = "TenantAdmin";
    public const string Staff = "Staff";
    public const string Customer = "Customer";

    public static readonly string[] All =
    {
        SuperAdmin,
        TenantAdmin,
        Staff,
        Customer
    };
}
