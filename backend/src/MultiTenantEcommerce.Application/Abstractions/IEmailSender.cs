namespace MultiTenantEcommerce.Application.Abstractions;

public interface IEmailSender
{
    Task SendAsync(string email, string subject, string body, CancellationToken cancellationToken = default);
}
