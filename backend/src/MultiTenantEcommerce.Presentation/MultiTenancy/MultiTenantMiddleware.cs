using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.MultiTenancy;

public class MultiTenantMiddleware
{
    private readonly RequestDelegate _next;

    public MultiTenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(
        HttpContext context,
        MultiTenantContext tenantContext,
        AdminDbContext adminDbContext,
        IOptions<MultiTenancyOptions> options)
    {
        if (context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var tenant = await ResolveTenantAsync(context, adminDbContext);
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Unable to resolve tenant from request." });
            return;
        }

        if (!tenant.IsActive)
        {
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant is inactive." });
            return;
        }

        var multiTenancyOptions = options.Value;
        tenantContext.CurrentTenantId = tenant.Id;
        tenantContext.TenantIdentifier = tenant.Subdomain;
        tenantContext.ConnectionString = multiTenancyOptions.UseSharedDatabase
            ? ResolveSharedConnectionString(multiTenancyOptions)
            : tenant.DbConnectionString;

        await _next(context);
    }

    private static async Task<Tenant?> ResolveTenantAsync(HttpContext context, AdminDbContext adminDbContext)
    {
        var cancellationToken = context.RequestAborted;

        if (context.Request.Headers.TryGetValue("X-Tenant", out var headerValue))
        {
            var identifier = headerValue.ToString().Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(identifier))
            {
                return await adminDbContext.Tenants.AsNoTracking()
                    .FirstOrDefaultAsync(t => t.Subdomain == identifier || t.CustomDomain == identifier, cancellationToken);
            }
        }

        var host = context.Request.Host.Host?.Trim().ToLowerInvariant();
        if (string.IsNullOrEmpty(host))
        {
            return null;
        }

        var customDomainMatch = await adminDbContext.Tenants.AsNoTracking()
            .FirstOrDefaultAsync(t => t.CustomDomain == host, cancellationToken);
        if (customDomainMatch is not null)
        {
            return customDomainMatch;
        }

        var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length >= 2)
        {
            var subdomain = segments[0];
            return await adminDbContext.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Subdomain == subdomain, cancellationToken);
        }

        return null;
    }

    private static string ResolveSharedConnectionString(MultiTenancyOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.SharedDatabaseConnectionString))
        {
            return options.SharedDatabaseConnectionString;
        }

        if (!string.IsNullOrWhiteSpace(options.AdminConnectionString))
        {
            return options.AdminConnectionString;
        }

        throw new InvalidOperationException("Shared database connection string is not configured.");
    }
}
