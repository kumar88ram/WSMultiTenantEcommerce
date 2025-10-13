using MultiTenantEcommerce.Application.Models.Catalog;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IProductCatalogService
{
    Task<ProductListResult> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default);
    Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<ProductVariantDto> CreateVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default);
    Task<InventoryDto> UpdateInventoryAsync(Guid variantId, UpdateInventoryRequest request, CancellationToken cancellationToken = default);
    Task<BulkImportResult> ImportProductsFromCsvAsync(Stream csvStream, bool useStoredProcedure, CancellationToken cancellationToken = default);
}
