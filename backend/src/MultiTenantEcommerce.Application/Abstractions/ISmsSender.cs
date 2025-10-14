namespace MultiTenantEcommerce.Application.Abstractions;

public interface ISmsSender
{
    Task SendAsync(string phoneNumber, string message, CancellationToken cancellationToken = default);
}
