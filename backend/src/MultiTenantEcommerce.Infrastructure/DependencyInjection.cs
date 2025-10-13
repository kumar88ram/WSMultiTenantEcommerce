using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Services;
using MultiTenantEcommerce.Infrastructure.Persistence;
using MultiTenantEcommerce.Infrastructure.Security;

namespace MultiTenantEcommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenFactory, JwtTokenFactory>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        return services;
    }
}
