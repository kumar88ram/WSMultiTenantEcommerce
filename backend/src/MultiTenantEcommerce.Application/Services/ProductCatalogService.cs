using System.Globalization;
using System.Text;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Catalog;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Services;

public class ProductCatalogService : IProductCatalogService
{
    private const int MaxPageSize = 100;

    private readonly IProductRepository _productRepository;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<ProductCatalogService> _logger;

    public ProductCatalogService(
        IProductRepository productRepository,
        ITenantResolver tenantResolver,
        ILogger<ProductCatalogService> logger)
    {
        _productRepository = productRepository;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    public async Task<ProductListResult> GetProductsAsync(ProductListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, MaxPageSize);
        var filters = query.AttributeFilters ?? new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

        var filter = new ProductFilter(page, pageSize, query.CategorySlug, query.MinPrice, query.MaxPrice, filters);
        var (products, totalCount) = await _productRepository.GetProductsAsync(filter, cancellationToken);

        var items = products.Select(product =>
        {
            var categories = product.ProductCategories
                .Select(pc => pc.Category)
                .Where(c => c is not null)
                .Select(c => c!.Name)
                .Distinct()
                .ToList();

            var variantPrices = product.Variants
                .Where(v => v.IsActive)
                .Select(v => v.Price)
                .ToList();

            decimal? lowestVariant = variantPrices.Count > 0 ? variantPrices.Min() : null;
            decimal? highestVariant = variantPrices.Count > 0 ? variantPrices.Max() : null;

            return new ProductListItemDto(
                product.Id,
                product.Name,
                product.Slug,
                product.Price,
                product.CompareAtPrice,
                product.IsPublished,
                categories,
                lowestVariant,
                highestVariant);
        }).ToList();

        return new ProductListResult(page, pageSize, totalCount, items);
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return null;
        }

        var product = await _productRepository.GetProductBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return null;
        }

        return MapProductDetail(product);
    }

    public async Task<ProductVariantDto> CreateVariantAsync(Guid productId, CreateProductVariantRequest request, CancellationToken cancellationToken = default)
    {
        var product = await _productRepository.GetProductAggregateAsync(productId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found");

        if (!product.Attributes.Any())
        {
            throw new InvalidOperationException("Product has no attributes defined");
        }

        var variant = new ProductVariant
        {
            TenantId = product.TenantId,
            ProductId = product.Id,
            Product = product,
            Name = request.Name,
            Sku = request.Sku,
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            IsActive = request.IsActive
        };

        foreach (var (attributeName, value) in request.AttributeSelections)
        {
            var attribute = product.Attributes
                .FirstOrDefault(a => string.Equals(a.Name, attributeName, StringComparison.OrdinalIgnoreCase));

            if (attribute is null)
            {
                throw new InvalidOperationException($"Attribute '{attributeName}' is not defined for this product");
            }

            var attributeValue = attribute.Values
                .FirstOrDefault(v => string.Equals(v.Value, value, StringComparison.OrdinalIgnoreCase));

            if (attributeValue is null)
            {
                attributeValue = new AttributeValue
                {
                    TenantId = product.TenantId,
                    ProductAttributeId = attribute.Id,
                    Value = value,
                    SortOrder = attribute.Values.Count
                };
                attribute.Values.Add(attributeValue);
            }

            variant.AttributeValues.Add(new ProductVariantAttributeValue
            {
                TenantId = product.TenantId,
                AttributeValue = attributeValue,
                AttributeValueId = attributeValue.Id,
                ProductVariant = variant
            });
        }

        var inventory = new Inventory
        {
            TenantId = product.TenantId,
            ProductId = product.Id,
            Product = product,
            QuantityOnHand = request.QuantityOnHand,
            ReservedQuantity = request.ReservedQuantity
        };

        variant.Inventory = inventory;

        var savedVariant = await _productRepository.CreateVariantWithInventoryAsync(product, variant, inventory, cancellationToken);
        _logger.LogInformation("Variant {Variant} created for product {Product}", savedVariant.Id, product.Id);

        return MapVariant(savedVariant);
    }

    public async Task<InventoryDto> UpdateInventoryAsync(Guid variantId, UpdateInventoryRequest request, CancellationToken cancellationToken = default)
    {
        var inventory = await _productRepository.UpdateInventoryAsync(variantId, request.QuantityOnHand, request.ReservedQuantity, cancellationToken);
        return MapInventory(inventory);
    }

    public async Task<BulkImportResult> ImportProductsFromCsvAsync(Stream csvStream, bool useStoredProcedure, CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(csvStream, Encoding.UTF8, true, leaveOpen: false);
        var lineNumber = 0;
        var errors = new List<string>();
        var validProducts = new List<Product>();
        var tenantId = _tenantResolver.CurrentTenantId;

        string? headerLine = await reader.ReadLineAsync();
        lineNumber++;
        if (headerLine is null)
        {
            return new BulkImportResult(0, 0, new[] { "CSV file is empty." });
        }

        var headers = ParseCsvLine(headerLine).ToArray();
        var expectedHeaders = new[]
        {
            "Name", "Slug", "Description", "Price", "CompareAtPrice", "IsPublished", "Categories", "Attributes", "InventoryQuantity"
        };

        if (!headers.SequenceEqual(expectedHeaders, StringComparer.OrdinalIgnoreCase))
        {
            return new BulkImportResult(0, 0, new[] { "CSV header row does not match the expected format." });
        }

        string? line;
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line).ToArray();
            if (fields.Length != expectedHeaders.Length)
            {
                errors.Add($"Line {lineNumber}: Expected {expectedHeaders.Length} columns but found {fields.Length}.");
                continue;
            }

            if (!decimal.TryParse(fields[3], NumberStyles.Number, CultureInfo.InvariantCulture, out var price))
            {
                errors.Add($"Line {lineNumber}: Invalid price.");
                continue;
            }

            decimal? compareAtPrice = null;
            if (!string.IsNullOrWhiteSpace(fields[4]))
            {
                if (decimal.TryParse(fields[4], NumberStyles.Number, CultureInfo.InvariantCulture, out var cap))
                {
                    compareAtPrice = cap;
                }
                else
                {
                    errors.Add($"Line {lineNumber}: Invalid compare-at price.");
                    continue;
                }
            }

            if (!bool.TryParse(fields[5], out var isPublished))
            {
                errors.Add($"Line {lineNumber}: Invalid publication flag.");
                continue;
            }

            if (!int.TryParse(fields[8], NumberStyles.Integer, CultureInfo.InvariantCulture, out var inventoryQuantity) || inventoryQuantity < 0)
            {
                errors.Add($"Line {lineNumber}: Invalid inventory quantity.");
                continue;
            }

            var slug = fields[1];
            if (!seenSlugs.Add(slug))
            {
                errors.Add($"Line {lineNumber}: Duplicate slug '{slug}' within the file.");
                continue;
            }

            var categories = SplitDelimitedList(fields[6]);
            var attributes = ParseAttributePayload(fields[7]);

            var product = new Product
            {
                TenantId = tenantId,
                Name = fields[0],
                Slug = slug,
                Description = string.IsNullOrWhiteSpace(fields[2]) ? null : fields[2],
                Price = price,
                CompareAtPrice = compareAtPrice,
                IsPublished = isPublished,
            };

            foreach (var (attributeName, values) in attributes)
            {
                var attribute = new ProductAttribute
                {
                    TenantId = tenantId,
                    Product = product,
                    Name = attributeName,
                    DisplayName = attributeName
                };

                foreach (var value in values)
                {
                    attribute.Values.Add(new AttributeValue
                    {
                        TenantId = tenantId,
                        ProductAttribute = attribute,
                        Value = value,
                        SortOrder = attribute.Values.Count
                    });
                }

                product.Attributes.Add(attribute);
            }

            product.Inventory.Add(new Inventory
            {
                TenantId = tenantId,
                Product = product,
                QuantityOnHand = inventoryQuantity,
                ReservedQuantity = 0
            });

            product.ProductCategories = categories
                .Select(slugValue => new ProductCategory
                {
                    TenantId = tenantId,
                    Product = product,
                    Category = new Category { TenantId = tenantId, Slug = slugValue, Name = slugValue }
                })
                .ToList();

            validProducts.Add(product);
        }

        var imported = 0;
        if (validProducts.Count > 0)
        {
            await NormalizeCategoriesAsync(validProducts, errors, cancellationToken);

            if (useStoredProcedure)
            {
                var bulkRows = validProducts.Select(product =>
                {
                    var categoryString = string.Join(';', product.ProductCategories
                        .Select(pc => pc.Category)
                        .Where(c => c is not null)
                        .Select(c => c!.Slug));

                    var attributePayload = BuildAttributePayload(product);
                    var inventoryQuantity = product.Inventory.FirstOrDefault()?.QuantityOnHand ?? 0;

                    return new ProductBulkImportRow(
                        tenantId,
                        product.Name,
                        product.Slug,
                        product.Description,
                        product.Price,
                        product.CompareAtPrice,
                        product.IsPublished,
                        categoryString,
                        attributePayload,
                        inventoryQuantity);
                }).ToList();

                if (bulkRows.Count > 0)
                {
                    await _productRepository.BulkImportUsingStoredProcedureAsync(bulkRows, cancellationToken);
                }

                imported = bulkRows.Count;
            }
            else
            {
                await _productRepository.UpsertProductsAsync(validProducts, cancellationToken);
                imported = validProducts.Count;
            }
        }

        var failed = errors.Count;
        return new BulkImportResult(imported, failed, errors);
    }

    private async Task NormalizeCategoriesAsync(IEnumerable<Product> products, List<string> errors, CancellationToken cancellationToken)
    {
        var slugs = products
            .SelectMany(p => p.ProductCategories.Select(pc => pc.Category.Slug))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (slugs.Count == 0)
        {
            return;
        }

        var existing = await _productRepository.GetCategoriesBySlugsAsync(slugs, cancellationToken);
        foreach (var product in products)
        {
            foreach (var productCategory in product.ProductCategories.ToList())
            {
                if (existing.TryGetValue(productCategory.Category.Slug, out var category))
                {
                    productCategory.Category = category;
                    productCategory.CategoryId = category.Id;
                    productCategory.TenantId = category.TenantId;
                }
                else
                {
                    product.ProductCategories.Remove(productCategory);
                    _logger.LogWarning(
                        "Category {CategorySlug} does not exist for tenant {Tenant}. Skipping association for product {Product}.",
                        productCategory.Category.Slug,
                        _tenantResolver.CurrentTenantId,
                        product.Slug);
                    errors.Add($"Category '{productCategory.Category.Slug}' does not exist for tenant '{_tenantResolver.CurrentTenantId}'. Association skipped for product '{product.Slug}'.");
                }
            }
        }
    }

    private static ProductDetailDto MapProductDetail(Product product)
    {
        var categories = product.ProductCategories
            .Select(pc => pc.Category)
            .Where(c => c is not null)
            .Select(c => c!.Name)
            .Distinct()
            .ToList();

        var attributes = product.Attributes
            .Select(attribute => new ProductAttributeDto(
                attribute.Id,
                attribute.Name,
                attribute.DisplayName,
                attribute.Values
                    .OrderBy(v => v.SortOrder)
                    .Select(v => new AttributeValueDto(v.Id, v.Value, v.SortOrder))
                    .ToList()))
            .ToList();

        var variants = product.Variants
            .Select(MapVariant)
            .ToList();

        var inventory = product.Inventory.FirstOrDefault();
        var inventoryDto = inventory is null ? null : MapInventory(inventory);

        return new ProductDetailDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.CompareAtPrice,
            product.IsPublished,
            categories,
            attributes,
            variants,
            inventoryDto);
    }

    private static ProductVariantDto MapVariant(ProductVariant variant)
    {
        var attributes = variant.AttributeValues
            .Select(v => new VariantAttributeDto(
                v.AttributeValue.ProductAttribute.Name,
                v.AttributeValue.Value))
            .ToList();

        var inventoryDto = variant.Inventory is null ? null : MapInventory(variant.Inventory);

        return new ProductVariantDto(
            variant.Id,
            variant.Name,
            variant.Sku,
            variant.Price,
            variant.CompareAtPrice,
            variant.IsActive,
            attributes,
            inventoryDto);
    }

    private static InventoryDto MapInventory(Inventory inventory)
    {
        var available = Math.Max(0, inventory.QuantityOnHand - inventory.ReservedQuantity);
        return new InventoryDto(inventory.QuantityOnHand, inventory.ReservedQuantity, available, inventory.LastAdjustedAt);
    }

    private static IReadOnlyDictionary<string, string[]> ParseAttributePayload(string payload)
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return result;
        }

        var segments = payload.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var segment in segments)
        {
            var parts = segment.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var attribute = parts[0];
            var values = parts[1]
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            result[attribute] = values;
        }

        return result;
    }

    private static string SerializeAttributePayload(IReadOnlyDictionary<string, string[]> attributes)
    {
        if (attributes.Count == 0)
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        var isFirst = true;
        foreach (var (key, values) in attributes)
        {
            if (!isFirst)
            {
                builder.Append(';');
            }

            builder.Append(key);
            builder.Append('=');
            builder.Append(string.Join('|', values));
            isFirst = false;
        }

        return builder.ToString();
    }

    private static IEnumerable<string> SplitDelimitedList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Array.Empty<string>();
        }

        return value.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static IEnumerable<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    current.Append('\"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        values.Add(current.ToString());
        return values;
    }

    private static string BuildAttributePayload(Product product)
    {
        if (!product.Attributes.Any())
        {
            return string.Empty;
        }

        var attributeMap = product.Attributes.ToDictionary(
            a => a.Name,
            a => a.Values.Select(v => v.Value).ToArray(),
            StringComparer.OrdinalIgnoreCase);

        return SerializeAttributePayload(attributeMap);
    }
}
