using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.MultiTenancy;

public class MultiTenantMiddleware
{
    private readonly RequestDelegate _next;

    public MultiTenantMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, MultiTenantContext tenantContext, ApplicationDbContext dbContext)
    {
        var tenantIdentifier = ResolveTenantIdentifier(context);
        if (tenantIdentifier is null)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant header or subdomain is required" });
            return;
        }

        var tenant = await dbContext.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Identifier == tenantIdentifier);
        if (tenant is null)
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            await context.Response.WriteAsJsonAsync(new { error = "Tenant not found" });
            return;
        }

        tenantContext.CurrentTenantId = tenant.Id;
        tenantContext.TenantIdentifier = tenant.Identifier;

        await _next(context);
    }

    private static string? ResolveTenantIdentifier(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue("X-Tenant", out var headerValue))
        {
            return headerValue.ToString();
        }

        var host = context.Request.Host.Host;
        var segments = host.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length > 2)
        {
            return segments[0];
        }

        return null;
    }
}
