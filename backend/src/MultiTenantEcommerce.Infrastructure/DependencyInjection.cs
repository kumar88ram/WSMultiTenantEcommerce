using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Services;
using MultiTenantEcommerce.Infrastructure.BackgroundWorkers;
using MultiTenantEcommerce.Infrastructure.MultiTenancy;
using MultiTenantEcommerce.Infrastructure.Payments;
using MultiTenantEcommerce.Infrastructure.Persistence;
using MultiTenantEcommerce.Infrastructure.Persistence.Repositories;
using MultiTenantEcommerce.Infrastructure.Security;

namespace MultiTenantEcommerce.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<MultiTenancyOptions>(configuration.GetSection("MultiTenancy"));

        services.AddDbContext<AdminDbContext>((sp, options) =>
        {
            var multiTenancyOptions = sp.GetRequiredService<IOptions<MultiTenancyOptions>>().Value;
            var connectionString = !string.IsNullOrWhiteSpace(multiTenancyOptions.AdminConnectionString)
                ? multiTenancyOptions.AdminConnectionString
                : configuration.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Admin connection string is not configured.");

            options.UseSqlServer(connectionString, builder =>
                builder.MigrationsHistoryTable("__AdminMigrationsHistory"));
        });

        services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            var multiTenancyOptions = sp.GetRequiredService<IOptions<MultiTenancyOptions>>().Value;

            string connectionString;
            if (multiTenancyOptions.UseSharedDatabase)
            {
                connectionString = !string.IsNullOrWhiteSpace(multiTenancyOptions.SharedDatabaseConnectionString)
                    ? multiTenancyOptions.SharedDatabaseConnectionString
                    : configuration.GetConnectionString("DefaultConnection")
                      ?? throw new InvalidOperationException("Shared database connection string is not configured.");
            }
            else
            {
                var tenantResolver = sp.GetRequiredService<ITenantResolver>();
                connectionString = tenantResolver.ConnectionString
                                 ?? throw new InvalidOperationException("Tenant connection string was not resolved.");
            }

            options.UseSqlServer(connectionString);
        });

        services.AddScoped<ITenantDbContextFactory, TenantDbContextFactory>();
        services.AddScoped<ITenantProvisioningService, TenantProvisioningService>();
        services.AddScoped<IAdminTenantService, AdminTenantService>();
        services.AddScoped<ISubscriptionPlanService, SubscriptionPlanService>();
        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IPluginManagementService, PluginManagementService>();
        services.AddScoped<ICronJobService, CronJobService>();
        services.AddScoped<IAnalyticsService, AnalyticsService>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IProductCatalogService, ProductCatalogService>();
        services.AddScoped<ICheckoutService, CheckoutService>();
        services.AddSingleton<IEmailNotificationQueue, EmailNotificationQueue>();
        services.AddSingleton<IEmailNotificationSender, LoggingEmailNotificationSender>();
        services.AddSingleton<IPaymentGatewayClient, StripeLikePaymentGatewayClient>();
        services.AddHostedService<EmailNotificationWorker>();

        services.AddScoped<IPasswordHasher, PasswordHasher>();
        services.AddScoped<ITokenFactory, JwtTokenFactory>();
        services.AddScoped<IAuthService, AuthService>();

        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));

        return services;
    }
}
