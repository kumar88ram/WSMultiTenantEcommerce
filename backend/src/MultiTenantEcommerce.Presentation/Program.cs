using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using MultiTenantEcommerce.Infrastructure;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Presentation.MultiTenancy;
using MultiTenantEcommerce.Infrastructure.Persistence;
using MultiTenantEcommerce.Infrastructure.MultiTenancy;
using MultiTenantEcommerce.Presentation.Middleware;
using MultiTenantEcommerce.Presentation.Security;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMultiTenancy();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.Configure<RateLimitingOptions>(builder.Configuration.GetSection("RateLimiting"));

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
}).AddJwtBearer(options =>
{
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("JWT signing key is not configured");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSection["Issuer"],
        ValidAudience = jwtSection["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey))
    };
});

builder.Services.AddAuthorization(AuthorizationPolicies.ConfigurePolicies);

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var adminDb = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
    if (adminDb.Database.GetPendingMigrations().Any())
    {
        adminDb.Database.Migrate();
    }

    var multiTenancyOptions = scope.ServiceProvider.GetRequiredService<IOptions<MultiTenancyOptions>>().Value;
    if (multiTenancyOptions.UseSharedDatabase)
    {
        var tenantDbFactory = scope.ServiceProvider.GetRequiredService<ITenantDbContextFactory>();
        var sharedConnectionString = string.IsNullOrWhiteSpace(multiTenancyOptions.SharedDatabaseConnectionString)
            ? multiTenancyOptions.AdminConnectionString
            : multiTenancyOptions.SharedDatabaseConnectionString;

        if (string.IsNullOrWhiteSpace(sharedConnectionString))
        {
            throw new InvalidOperationException("Shared database connection string is not configured.");
        }

        await using var sharedDb = tenantDbFactory.CreateDbContext(sharedConnectionString, Guid.Empty);
        if (await sharedDb.Database.GetPendingMigrationsAsync() is { } pending && pending.Any())
        {
            await sharedDb.Database.MigrateAsync();
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseMultiTenancy();
app.UseMiddleware<ThemeResolverMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
