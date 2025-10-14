namespace MultiTenantEcommerce.Presentation.Security;

public class RateLimitingOptions
{
    public int RequestsPerIp { get; set; } = 100;
    public int RequestsPerTenant { get; set; } = 1000;
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
