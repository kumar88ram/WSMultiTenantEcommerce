using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Infrastructure.BackgroundWorkers;

public class EmailNotificationWorker : BackgroundService
{
    private readonly IEmailNotificationQueue _queue;
    private readonly IEmailNotificationSender _sender;
    private readonly ILogger<EmailNotificationWorker> _logger;

    public EmailNotificationWorker(
        IEmailNotificationQueue queue,
        IEmailNotificationSender sender,
        ILogger<EmailNotificationWorker> logger)
    {
        _queue = queue;
        _sender = sender;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var notification in _queue.DequeueAsync(stoppingToken))
        {
            try
            {
                await _sender.SendAsync(notification, stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email notification for order {OrderNumber}", notification.OrderNumber);
            }
        }
    }
}
