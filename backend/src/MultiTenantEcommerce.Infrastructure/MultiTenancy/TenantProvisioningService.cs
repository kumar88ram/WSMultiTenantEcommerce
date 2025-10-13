using System.Text.RegularExpressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.MultiTenancy;

internal sealed partial class TenantProvisioningService : ITenantProvisioningService
{
    private readonly AdminDbContext _adminDbContext;
    private readonly ITenantDbContextFactory _tenantDbContextFactory;
    private readonly MultiTenancyOptions _options;
    private readonly ILogger<TenantProvisioningService> _logger;

    public TenantProvisioningService(
        AdminDbContext adminDbContext,
        ITenantDbContextFactory tenantDbContextFactory,
        IOptions<MultiTenancyOptions> options,
        ILogger<TenantProvisioningService> logger)
    {
        _adminDbContext = adminDbContext;
        _tenantDbContextFactory = tenantDbContextFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<Tenant> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        await EnsureUniqueTenantAsync(request, cancellationToken);

        var planExists = await _adminDbContext.SubscriptionPlans
            .AnyAsync(p => p.Id == request.PlanId, cancellationToken);

        if (!planExists)
        {
            throw new ArgumentException("Subscription plan does not exist.", nameof(request));
        }

        var tenant = new Tenant
        {
            Name = request.Name.Trim(),
            Subdomain = request.Subdomain.Trim().ToLowerInvariant(),
            CustomDomain = string.IsNullOrWhiteSpace(request.CustomDomain) ? null : request.CustomDomain.Trim().ToLowerInvariant(),
            PlanId = request.PlanId,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        if (_options.UseSharedDatabase)
        {
            tenant.DbConnectionString = ResolveSharedConnectionString();
            _logger.LogInformation("Provisioned tenant {Tenant} using shared database strategy.", tenant.Subdomain);
        }
        else
        {
            tenant.DbConnectionString = await ProvisionDatabaseForTenantAsync(tenant, cancellationToken);
            _logger.LogInformation("Provisioned dedicated database for tenant {Tenant}.", tenant.Subdomain);
        }

        _adminDbContext.Tenants.Add(tenant);
        await _adminDbContext.SaveChangesAsync(cancellationToken);

        return tenant;
    }

    private async Task<string> ProvisionDatabaseForTenantAsync(Tenant tenant, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.MasterConnectionString))
        {
            throw new InvalidOperationException("Multi-tenancy master connection string is not configured.");
        }

        if (string.IsNullOrWhiteSpace(_options.TenantConnectionStringTemplate))
        {
            throw new InvalidOperationException("Tenant connection string template is not configured.");
        }

        var databaseName = $"Tenant_{tenant.Subdomain}_{tenant.Id:N}";

        await CreateDatabaseIfNotExistsAsync(databaseName, cancellationToken);

        var tenantConnectionString = _options.TenantConnectionStringTemplate
            .Replace("{Database}", databaseName, StringComparison.OrdinalIgnoreCase)
            .Replace("{TenantId}", tenant.Id.ToString("N"), StringComparison.OrdinalIgnoreCase);

        await RunMigrationsAsync(tenantConnectionString, tenant, cancellationToken);

        return tenantConnectionString;
    }

    private async Task RunMigrationsAsync(string connectionString, Tenant tenant, CancellationToken cancellationToken)
    {
        await using var tenantDbContext = _tenantDbContextFactory.CreateDbContext(connectionString, tenant.Id, tenant.Subdomain);
        await tenantDbContext.Database.MigrateAsync(cancellationToken);
    }

    private async Task CreateDatabaseIfNotExistsAsync(string databaseName, CancellationToken cancellationToken)
    {
        await using var connection = new SqlConnection(_options.MasterConnectionString);
        await connection.OpenAsync(cancellationToken);

        var commandText = $$"\nIF DB_ID('{databaseName}') IS NULL\nBEGIN\n    DECLARE @sql NVARCHAR(MAX) = 'CREATE DATABASE [{databaseName}]';\n    EXEC (@sql);\nEND\n"$$;

        await using var command = new SqlCommand(commandText, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private async Task EnsureUniqueTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken)
    {
        var normalizedSubdomain = request.Subdomain.Trim().ToLowerInvariant();
        var normalizedCustomDomain = string.IsNullOrWhiteSpace(request.CustomDomain)
            ? null
            : request.CustomDomain.Trim().ToLowerInvariant();

        var exists = await _adminDbContext.Tenants
            .AnyAsync(t => t.Subdomain == normalizedSubdomain
                           || (normalizedCustomDomain != null && t.CustomDomain == normalizedCustomDomain), cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException("Tenant with the same subdomain or custom domain already exists.");
        }
    }

    private static void ValidateRequest(CreateTenantRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Tenant name is required.", nameof(request));
        }

        if (!SubdomainRegex().IsMatch(request.Subdomain))
        {
            throw new ArgumentException("Subdomain is invalid.", nameof(request));
        }

        if (!string.IsNullOrWhiteSpace(request.CustomDomain) && !DomainRegex().IsMatch(request.CustomDomain))
        {
            throw new ArgumentException("Custom domain is invalid.", nameof(request));
        }

        if (request.PlanId == Guid.Empty)
        {
            throw new ArgumentException("Plan identifier is required.", nameof(request));
        }
    }

    private string ResolveSharedConnectionString()
    {
        if (!string.IsNullOrWhiteSpace(_options.SharedDatabaseConnectionString))
        {
            return _options.SharedDatabaseConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(_options.AdminConnectionString))
        {
            return _options.AdminConnectionString;
        }

        throw new InvalidOperationException("Shared database connection string is not configured.");
    }

    [GeneratedRegex("^[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SubdomainRegex();

    [GeneratedRegex("^(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\\.)+[a-z]{2,}$", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DomainRegex();
}
