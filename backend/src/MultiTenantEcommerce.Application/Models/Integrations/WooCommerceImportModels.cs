namespace MultiTenantEcommerce.Application.Models.Integrations;

public enum WooCommerceImportFormat
{
    Csv,
    Json
}

public record WooCommerceImportResult(
    int ImportedCount,
    int SkippedCount,
    IReadOnlyCollection<string> Errors);
