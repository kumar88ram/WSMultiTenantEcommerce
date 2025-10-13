namespace MultiTenantEcommerce.Application.Models.Catalog;

public record ProductListQuery(
    int Page = 1,
    int PageSize = 25,
    string? CategorySlug = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    IReadOnlyDictionary<string, string[]>? AttributeFilters = null);

public record ProductFilter(
    int Page,
    int PageSize,
    string? CategorySlug,
    decimal? MinPrice,
    decimal? MaxPrice,
    IReadOnlyDictionary<string, string[]> AttributeFilters);

public record ProductBulkImportRow(
    Guid TenantId,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsPublished,
    string CategorySlugs,
    string AttributePayload,
    int InventoryQuantity);
