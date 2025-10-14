using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Application.Security;

public class AuditLoggingExamples
{
    private readonly IAuditLogger _auditLogger;

    public AuditLoggingExamples(IAuditLogger auditLogger)
    {
        _auditLogger = auditLogger;
    }

    public Task LogProductUpdateAsync(Guid? userId, object before, object after, CancellationToken cancellationToken = default)
    {
        return _auditLogger.LogAsync(userId, "Update", "Product", before, after, cancellationToken);
    }

    public Task LogUserDeletionAsync(Guid? userId, object deletedUser, CancellationToken cancellationToken = default)
    {
        return _auditLogger.LogAsync(userId, "Delete", "User", deletedUser, null, cancellationToken);
    }
}
