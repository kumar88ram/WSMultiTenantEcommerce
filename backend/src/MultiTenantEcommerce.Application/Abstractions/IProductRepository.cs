using MultiTenantEcommerce.Application.Models.Catalog;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IProductRepository
{
    Task<(IReadOnlyList<Product> Products, int TotalCount)> GetProductsAsync(ProductFilter filter, CancellationToken cancellationToken = default);
    Task<Product?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<Product?> GetProductAggregateAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<ProductVariant> CreateVariantWithInventoryAsync(Product product, ProductVariant variant, Inventory inventory, CancellationToken cancellationToken = default);
    Task<Inventory> UpdateInventoryAsync(Guid variantId, int quantityOnHand, int reservedQuantity, CancellationToken cancellationToken = default);
    Task<IDictionary<string, Category>> GetCategoriesBySlugsAsync(IEnumerable<string> slugs, CancellationToken cancellationToken = default);
    Task UpsertProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default);
    Task BulkImportUsingStoredProcedureAsync(IEnumerable<ProductBulkImportRow> rows, CancellationToken cancellationToken = default);
}
