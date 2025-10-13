using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.MultiTenancy;

public interface ITenantDbContextFactory
{
    ApplicationDbContext CreateDbContext(string connectionString, Guid tenantId, string? identifier = null);
}

internal sealed class TenantDbContextFactory : ITenantDbContextFactory
{
    private readonly IOptions<MultiTenancyOptions> _multiTenancyOptions;

    public TenantDbContextFactory(IOptions<MultiTenancyOptions> multiTenancyOptions)
    {
        _multiTenancyOptions = multiTenancyOptions;
    }

    public ApplicationDbContext CreateDbContext(string connectionString, Guid tenantId, string? identifier = null)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(connectionString);

        var resolver = new ProvisioningTenantResolver(tenantId, identifier, connectionString);
        return new ApplicationDbContext(optionsBuilder.Options, resolver, _multiTenancyOptions);
    }
}
