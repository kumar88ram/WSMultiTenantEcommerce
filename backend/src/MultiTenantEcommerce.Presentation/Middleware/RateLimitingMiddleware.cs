using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Memory;
using MultiTenantEcommerce.Presentation.MultiTenancy;
using MultiTenantEcommerce.Presentation.Security;

namespace MultiTenantEcommerce.Presentation.Middleware;

public class RateLimitingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IMemoryCache _cache;
    private readonly RateLimitingOptions _options;

    private sealed class RateLimitEntry
    {
        public DateTime WindowStart { get; set; } = DateTime.UtcNow;
        public int Count { get; set; }
    }

    public RateLimitingMiddleware(
        RequestDelegate next,
        IMemoryCache cache,
        IOptions<RateLimitingOptions> options)
    {
        _next = next;
        _cache = cache;
        _options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context, MultiTenantContext tenantContext)
    {
        if (context.Request.Path.StartsWithSegments("/admin", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        if (!ProcessLimiter(BuildIpKey(context), _options.RequestsPerIp))
        {
            await RejectAsync(context, "IP address", _options.Window);
            return;
        }

        if (tenantContext.CurrentTenantId != Guid.Empty && !ProcessLimiter(BuildTenantKey(tenantContext.CurrentTenantId), _options.RequestsPerTenant))
        {
            await RejectAsync(context, "tenant", _options.Window);
            return;
        }

        await _next(context);
    }

    private bool ProcessLimiter(string key, int limit)
    {
        if (limit <= 0)
        {
            return true;
        }

        var entry = _cache.GetOrCreate(key, cacheEntry =>
        {
            cacheEntry.AbsoluteExpirationRelativeToNow = _options.Window;
            return new RateLimitEntry();
        });

        lock (entry)
        {
            var now = DateTime.UtcNow;
            if (now - entry.WindowStart >= _options.Window)
            {
                entry.WindowStart = now;
                entry.Count = 0;
            }

            entry.Count++;
            if (entry.Count > limit)
            {
                return false;
            }

            return true;
        }
    }

    private static Task RejectAsync(HttpContext context, string scope, TimeSpan window)
    {
        context.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        context.Response.Headers.RetryAfter = Math.Ceiling(window.TotalSeconds).ToString();
        return context.Response.WriteAsJsonAsync(new
        {
            error = $"Rate limit exceeded for this {scope}. Please try again later."
        });
    }

    private static string BuildIpKey(HttpContext context)
    {
        var ipAddress = context.Connection.RemoteIpAddress?.ToString();
        return string.IsNullOrWhiteSpace(ipAddress) ? "rate:ip:unknown" : $"rate:ip:{ipAddress}";
    }

    private static string BuildTenantKey(Guid tenantId)
    {
        return $"rate:tenant:{tenantId}";
    }
}
