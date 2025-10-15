using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Presentation.Middleware;

public class ThemeResolverMiddleware
{
    public const string HttpContextItemKey = "ActiveTenantTheme";

    private readonly RequestDelegate _next;

    public ThemeResolverMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, ITenantResolver tenantResolver, IThemeService themeService)
    {
        if (tenantResolver.CurrentTenantId != Guid.Empty)
        {
            var activeTheme = await themeService.GetActiveThemeAsync(tenantResolver.CurrentTenantId, context.RequestAborted);
            if (activeTheme is not null)
            {
                context.Items[HttpContextItemKey] = activeTheme;
            }
        }

        await _next(context);
    }
}
