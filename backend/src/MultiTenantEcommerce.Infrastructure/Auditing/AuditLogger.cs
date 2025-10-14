using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Infrastructure.Auditing;

public class AuditLogger : IAuditLogger
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<AuditLogger> _logger;
    private readonly JsonSerializerOptions _serializerOptions;

    public AuditLogger(
        ApplicationDbContext dbContext,
        ITenantResolver tenantResolver,
        ILogger<AuditLogger> logger)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
        _logger = logger;
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
    }

    public async Task LogAsync(
        Guid? userId,
        string action,
        string entityName,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(action))
        {
            throw new ArgumentException("Action must be provided for audit logging.", nameof(action));
        }

        if (string.IsNullOrWhiteSpace(entityName))
        {
            throw new ArgumentException("Entity name must be provided for audit logging.", nameof(entityName));
        }

        var tenantId = _tenantResolver.CurrentTenantId;
        if (tenantId == Guid.Empty)
        {
            _logger.LogWarning("Attempted to log audit event without a resolved tenant. Action: {Action}, Entity: {Entity}", action, entityName);
            return;
        }

        var entry = new AuditLog
        {
            TenantId = tenantId,
            UserId = userId,
            Action = action,
            EntityName = entityName,
            OldValues = Serialize(oldValues),
            NewValues = Serialize(newValues),
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AuditLogs.Add(entry);

        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            _logger.LogInformation(
                "Audit log persisted for tenant {TenantId}. Action: {Action}, Entity: {Entity}, User: {UserId}",
                tenantId,
                action,
                entityName,
                userId);
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError(ex, "Failed to persist audit log for tenant {TenantId}. Action: {Action}, Entity: {Entity}", tenantId, action, entityName);
            throw;
        }
    }

    private string? Serialize(object? value)
    {
        return value is null ? null : JsonSerializer.Serialize(value, _serializerOptions);
    }
}
