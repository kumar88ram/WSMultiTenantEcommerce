namespace MultiTenantEcommerce.Application.Abstractions;

public interface IAuditLogger
{
    Task LogAsync(
        Guid? userId,
        string action,
        string entityName,
        object? oldValues,
        object? newValues,
        CancellationToken cancellationToken = default);
}
