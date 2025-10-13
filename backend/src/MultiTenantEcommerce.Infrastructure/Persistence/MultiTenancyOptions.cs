namespace MultiTenantEcommerce.Infrastructure.Persistence;

public class MultiTenancyOptions
{
    public bool UseSharedDatabase { get; set; }
    public string AdminConnectionString { get; set; } = string.Empty;
    public string SharedDatabaseConnectionString { get; set; } = string.Empty;
    public string TenantConnectionStringTemplate { get; set; } = string.Empty;
    public string MasterConnectionString { get; set; } = string.Empty;
}
