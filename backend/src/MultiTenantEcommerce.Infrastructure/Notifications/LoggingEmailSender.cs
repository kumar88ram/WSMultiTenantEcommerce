using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Infrastructure.Notifications;

public class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending email to {Email}: {Subject} - {Body}", email, subject, body);
        return Task.CompletedTask;
    }
}
