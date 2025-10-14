using Microsoft.AspNetCore.Authorization;
using MultiTenantEcommerce.Domain.Security;

namespace MultiTenantEcommerce.Presentation.Security;

public static class AuthorizationPolicies
{
    public const string SuperAdminsOnly = "SuperAdminsOnly";
    public const string TenantAdminsOnly = "TenantAdminsOnly";
    public const string StaffAndAdmins = "StaffAndAdmins";
    public const string CustomerFacing = "CustomerFacing";

    public static void ConfigurePolicies(AuthorizationOptions options)
    {
        options.AddPolicy(SuperAdminsOnly, policy => policy.RequireRole(RoleNames.SuperAdmin));
        options.AddPolicy(TenantAdminsOnly, policy => policy.RequireRole(RoleNames.SuperAdmin, RoleNames.TenantAdmin));
        options.AddPolicy(StaffAndAdmins, policy => policy.RequireRole(RoleNames.SuperAdmin, RoleNames.TenantAdmin, RoleNames.Staff));
        options.AddPolicy(CustomerFacing, policy => policy.RequireRole(RoleNames.Customer, RoleNames.Staff, RoleNames.TenantAdmin, RoleNames.SuperAdmin));
    }
}
