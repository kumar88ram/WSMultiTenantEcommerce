using MultiTenantEcommerce.Application.Models.Integrations;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IWooCommerceImportService
{
    Task<WooCommerceImportResult> ImportAsync(Stream stream, WooCommerceImportFormat format, CancellationToken cancellationToken = default);
}
