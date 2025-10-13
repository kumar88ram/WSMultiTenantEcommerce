namespace MultiTenantEcommerce.Application.Models.Catalog;

public record ProductListResult(int Page, int PageSize, int TotalCount, IReadOnlyList<ProductListItemDto> Items);

public record ProductListItemDto(
    Guid Id,
    string Name,
    string Slug,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsPublished,
    IReadOnlyList<string> Categories,
    decimal? LowestVariantPrice,
    decimal? HighestVariantPrice);

public record ProductDetailDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsPublished,
    IReadOnlyList<string> Categories,
    IReadOnlyList<ProductAttributeDto> Attributes,
    IReadOnlyList<ProductVariantDto> Variants,
    InventoryDto? Inventory);

public record ProductAttributeDto(
    Guid Id,
    string Name,
    string DisplayName,
    IReadOnlyList<AttributeValueDto> Values);

public record AttributeValueDto(Guid Id, string Value, int SortOrder);

public record ProductVariantDto(
    Guid Id,
    string Name,
    string Sku,
    decimal Price,
    decimal? CompareAtPrice,
    bool IsActive,
    IReadOnlyList<VariantAttributeDto> Attributes,
    InventoryDto? Inventory);

public record VariantAttributeDto(string AttributeName, string AttributeValue);

public record InventoryDto(int QuantityOnHand, int ReservedQuantity, int AvailableQuantity, DateTime LastAdjustedAt);

public record BulkImportResult(int ImportedCount, int FailedCount, IReadOnlyList<string> Errors);
