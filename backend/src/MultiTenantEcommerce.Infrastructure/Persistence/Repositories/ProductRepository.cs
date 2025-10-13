using System.Data;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Catalog;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Persistence.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly ApplicationDbContext _dbContext;

    public ProductRepository(ApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<(IReadOnlyList<Product> Products, int TotalCount)> GetProductsAsync(ProductFilter filter, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Attributes)
                .ThenInclude(a => a.Values)
            .Include(p => p.Variants)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.CategorySlug))
        {
            query = query.Where(p => p.ProductCategories.Any(pc => pc.Category.Slug == filter.CategorySlug));
        }

        if (filter.MinPrice.HasValue)
        {
            query = query.Where(p => p.Price >= filter.MinPrice.Value);
        }

        if (filter.MaxPrice.HasValue)
        {
            query = query.Where(p => p.Price <= filter.MaxPrice.Value);
        }

        foreach (var (attribute, values) in filter.AttributeFilters)
        {
            if (values.Length == 0)
            {
                continue;
            }

            query = query.Where(p => p.Attributes.Any(a => a.Name == attribute && a.Values.Any(v => values.Contains(v.Value))));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var items = await query
            .OrderBy(p => p.Name)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<Product?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Attributes)
                .ThenInclude(a => a.Values)
            .Include(p => p.Variants)
                .ThenInclude(v => v.AttributeValues)
                    .ThenInclude(av => av.AttributeValue)
                        .ThenInclude(v => v.ProductAttribute)
            .Include(p => p.Variants)
                .ThenInclude(v => v.Inventory)
            .Include(p => p.Inventory)
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Slug == slug, cancellationToken);
    }

    public async Task<Product?> GetProductAggregateAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Products
            .Include(p => p.Attributes)
                .ThenInclude(a => a.Values)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);
    }

    public async Task<ProductVariant> CreateVariantWithInventoryAsync(Product product, ProductVariant variant, Inventory inventory, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            foreach (var attribute in product.Attributes)
            {
                foreach (var value in attribute.Values)
                {
                    if (_dbContext.Entry(value).State == EntityState.Detached)
                    {
                        _dbContext.Entry(value).State = EntityState.Added;
                    }
                }
            }

            inventory.ProductId = product.Id;
            inventory.ProductVariant = variant;
            inventory.ProductVariantId = variant.Id;

            variant.Inventory = inventory;
            _dbContext.ProductVariants.Add(variant);
            _dbContext.Inventories.Add(inventory);

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return await _dbContext.ProductVariants
                .Include(v => v.AttributeValues)
                    .ThenInclude(av => av.AttributeValue)
                        .ThenInclude(av => av.ProductAttribute)
                .Include(v => v.Inventory)
                .AsNoTracking()
                .FirstAsync(v => v.Id == variant.Id, cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<Inventory> UpdateInventoryAsync(Guid variantId, int quantityOnHand, int reservedQuantity, CancellationToken cancellationToken = default)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(i => i.ProductVariantId == variantId, cancellationToken)
                ?? throw new InvalidOperationException("Inventory record not found for variant.");

            inventory.QuantityOnHand = quantityOnHand;
            inventory.ReservedQuantity = reservedQuantity;
            inventory.LastAdjustedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return inventory;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IDictionary<string, Category>> GetCategoriesBySlugsAsync(IEnumerable<string> slugs, CancellationToken cancellationToken = default)
    {
        var normalized = slugs
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Select(s => s.Trim())
            .ToList();

        if (normalized.Count == 0)
        {
            return new Dictionary<string, Category>(StringComparer.OrdinalIgnoreCase);
        }

        var categories = await _dbContext.Categories
            .Where(c => normalized.Contains(c.Slug))
            .ToListAsync(cancellationToken);

        return categories.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);
    }

    public async Task UpsertProductsAsync(IEnumerable<Product> products, CancellationToken cancellationToken = default)
    {
        foreach (var product in products)
        {
            var existing = await _dbContext.Products
                .Include(p => p.ProductCategories)
                .Include(p => p.Attributes)
                    .ThenInclude(a => a.Values)
                .Include(p => p.Inventory)
                .FirstOrDefaultAsync(p => p.Slug == product.Slug && p.TenantId == product.TenantId, cancellationToken);

            if (existing is null)
            {
                _dbContext.Products.Add(product);
            }
            else
            {
                existing.Name = product.Name;
                existing.Description = product.Description;
                existing.Price = product.Price;
                existing.CompareAtPrice = product.CompareAtPrice;
                existing.IsPublished = product.IsPublished;

                existing.ProductCategories.Clear();
                foreach (var category in product.ProductCategories)
                {
                    existing.ProductCategories.Add(new ProductCategory
                    {
                        ProductId = existing.Id,
                        CategoryId = category.CategoryId,
                        Category = category.Category,
                        TenantId = existing.TenantId
                    });
                }

                existing.Attributes.Clear();
                foreach (var attribute in product.Attributes)
                {
                    var newAttribute = new ProductAttribute
                    {
                        Id = Guid.NewGuid(),
                        TenantId = existing.TenantId,
                        ProductId = existing.Id,
                        Name = attribute.Name,
                        DisplayName = attribute.DisplayName
                    };

                    foreach (var value in attribute.Values)
                    {
                        newAttribute.Values.Add(new AttributeValue
                        {
                            Id = Guid.NewGuid(),
                            TenantId = existing.TenantId,
                            ProductAttributeId = newAttribute.Id,
                            Value = value.Value,
                            SortOrder = value.SortOrder
                        });
                    }

                    existing.Attributes.Add(newAttribute);
                }

                existing.Inventory.Clear();
                foreach (var inventory in product.Inventory)
                {
                    existing.Inventory.Add(new Inventory
                    {
                        Id = Guid.NewGuid(),
                        TenantId = existing.TenantId,
                        ProductId = existing.Id,
                        QuantityOnHand = inventory.QuantityOnHand,
                        ReservedQuantity = inventory.ReservedQuantity,
                        LastAdjustedAt = DateTime.UtcNow
                    });
                }
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task BulkImportUsingStoredProcedureAsync(IEnumerable<ProductBulkImportRow> rows, CancellationToken cancellationToken = default)
    {
        var dataTable = new DataTable();
        dataTable.Columns.Add("TenantId", typeof(Guid));
        dataTable.Columns.Add("Name", typeof(string));
        dataTable.Columns.Add("Slug", typeof(string));
        dataTable.Columns.Add("Description", typeof(string));
        dataTable.Columns.Add("Price", typeof(decimal));
        dataTable.Columns.Add("CompareAtPrice", typeof(decimal));
        dataTable.Columns.Add("IsPublished", typeof(bool));
        dataTable.Columns.Add("CategorySlugs", typeof(string));
        dataTable.Columns.Add("AttributePayload", typeof(string));
        dataTable.Columns.Add("InventoryQuantity", typeof(int));

        foreach (var row in rows)
        {
            dataTable.Rows.Add(
                row.TenantId,
                row.Name,
                row.Slug,
                row.Description ?? (object)DBNull.Value,
                row.Price,
                row.CompareAtPrice ?? (object)DBNull.Value,
                row.IsPublished,
                row.CategorySlugs,
                row.AttributePayload,
                row.InventoryQuantity);
        }

        var parameter = new SqlParameter("@Products", SqlDbType.Structured)
        {
            TypeName = "dbo.ProductImportType",
            Value = dataTable
        };

        await _dbContext.Database.ExecuteSqlRawAsync("EXEC dbo.usp_BulkUpsertProducts @Products", new[] { parameter }, cancellationToken);
    }
}
