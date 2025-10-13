using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Presentation.MultiTenancy;

public static class MultiTenancyExtensions
{
    public static IApplicationBuilder UseMultiTenancy(this IApplicationBuilder app)
    {
        return app.UseMiddleware<MultiTenantMiddleware>();
    }

    public static IServiceCollection AddMultiTenancy(this IServiceCollection services)
    {
        services.AddScoped<MultiTenantContext>();
        services.AddScoped<ITenantResolver>(provider => provider.GetRequiredService<MultiTenantContext>());
        return services;
    }
}
