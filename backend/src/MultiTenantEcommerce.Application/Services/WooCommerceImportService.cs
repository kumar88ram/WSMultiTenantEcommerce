using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Constants;
using MultiTenantEcommerce.Application.Models.Integrations;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class WooCommerceImportService : IWooCommerceImportService
{
    private readonly ITenantResolver _tenantResolver;
    private readonly IPluginManagerService _pluginManagerService;
    private readonly IProductRepository _productRepository;
    private readonly ApplicationDbContext _dbContext;

    public WooCommerceImportService(
        ITenantResolver tenantResolver,
        IPluginManagerService pluginManagerService,
        IProductRepository productRepository,
        ApplicationDbContext dbContext)
    {
        _tenantResolver = tenantResolver;
        _pluginManagerService = pluginManagerService;
        _productRepository = productRepository;
        _dbContext = dbContext;
    }

    public async Task<WooCommerceImportResult> ImportAsync(Stream stream, WooCommerceImportFormat format, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        await _pluginManagerService.EnsurePluginEnabledAsync(tenantId, PluginSystemKeys.WooCommerceImport, cancellationToken);

        var parsedRows = format switch
        {
            WooCommerceImportFormat.Csv => await ParseCsvAsync(stream, cancellationToken),
            WooCommerceImportFormat.Json => await ParseJsonAsync(stream, cancellationToken),
            _ => throw new InvalidOperationException("Unsupported WooCommerce export format.")
        };

        var errors = new List<string>();
        var validRows = new List<WooCommerceParsedRow>();
        var seenSlugs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in parsedRows)
        {
            if (row.Errors.Count > 0)
            {
                errors.AddRange(row.Errors);
                continue;
            }

            var slug = string.IsNullOrWhiteSpace(row.Slug) ? Slugify(row.Name) : row.Slug;
            if (string.IsNullOrWhiteSpace(slug))
            {
                slug = $"product-{Guid.NewGuid():N}";
            }
            if (!seenSlugs.Add(slug))
            {
                errors.Add($"Duplicate slug '{slug}' detected in the import payload.");
                continue;
            }

            validRows.Add(row with { Slug = slug });
        }

        if (validRows.Count == 0)
        {
            return new WooCommerceImportResult(0, parsedRows.Count, errors);
        }

        var categoryNames = validRows
            .SelectMany(r => r.Categories)
            .Select(c => NormalizeCategorySlug(c))
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var existingCategories = categoryNames.Count == 0
            ? new List<Category>()
            : await _dbContext.Categories
                .Where(c => c.TenantId == tenantId && categoryNames.Contains(c.Slug))
                .ToListAsync(cancellationToken);

        var categoryMap = existingCategories.ToDictionary(c => c.Slug, StringComparer.OrdinalIgnoreCase);

        foreach (var slug in categoryNames)
        {
            if (categoryMap.ContainsKey(slug))
            {
                continue;
            }

            var name = ToTitleCase(slug.Replace('-', ' '));
            var category = new Category
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                Name = name,
                Slug = slug,
                Description = null,
            };

            _dbContext.Categories.Add(category);
            categoryMap[slug] = category;
        }

        if (_dbContext.ChangeTracker.HasChanges())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        var products = new List<Product>();

        foreach (var row in validRows)
        {
            var product = new Product
            {
                TenantId = tenantId,
                Name = row.Name,
                Slug = row.Slug,
                Description = row.Description ?? row.ShortDescription,
                Price = row.SalePrice ?? row.RegularPrice,
                CompareAtPrice = row.SalePrice.HasValue ? row.RegularPrice : null,
                IsPublished = row.Published,
            };

            foreach (var categoryName in row.Categories)
            {
                var slug = NormalizeCategorySlug(categoryName);
                if (string.IsNullOrWhiteSpace(slug) || !categoryMap.TryGetValue(slug, out var category))
                {
                    continue;
                }

                product.ProductCategories.Add(new ProductCategory
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    CategoryId = category.Id,
                    Category = category
                });
            }

            if (row.StockQuantity.HasValue)
            {
                product.Inventory.Add(new Inventory
                {
                    TenantId = tenantId,
                    ProductId = product.Id,
                    QuantityOnHand = Math.Max(0, row.StockQuantity.Value),
                    ReservedQuantity = 0,
                    LastAdjustedAt = DateTime.UtcNow
                });
            }

            products.Add(product);
        }

        if (products.Count == 0)
        {
            return new WooCommerceImportResult(0, parsedRows.Count, errors);
        }

        await _productRepository.UpsertProductsAsync(products, cancellationToken);

        var skippedCount = parsedRows.Count - products.Count;
        return new WooCommerceImportResult(products.Count, skippedCount, errors);
    }

    private async Task<List<WooCommerceParsedRow>> ParseCsvAsync(Stream stream, CancellationToken cancellationToken)
    {
        var rows = new List<WooCommerceParsedRow>();
        using var reader = new StreamReader(stream, Encoding.UTF8, true, leaveOpen: true);
        var lineNumber = 0;
        string? headerLine = await reader.ReadLineAsync();
        lineNumber++;
        if (headerLine is null)
        {
            return rows;
        }

        var headers = ParseCsvLine(headerLine).ToArray();
        var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < headers.Length; i++)
        {
            headerMap[headers[i]] = i;
        }

        string? line;
        while ((line = await reader.ReadLineAsync()) is not null)
        {
            lineNumber++;
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            var fields = ParseCsvLine(line).ToArray();
            var errors = new List<string>();

            string GetField(string name)
            {
                return headerMap.TryGetValue(name, out var index) && index < fields.Length ? fields[index] : string.Empty;
            }

            var name = GetField("Name");
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add($"Line {lineNumber}: Name is required.");
            }

            var regularPriceText = GetField("Regular price");
            var priceFallbackText = GetField("Price");
            decimal regularPrice;
            if (!decimal.TryParse(regularPriceText, NumberStyles.Number, CultureInfo.InvariantCulture, out regularPrice))
            {
                if (!decimal.TryParse(priceFallbackText, NumberStyles.Number, CultureInfo.InvariantCulture, out regularPrice))
                {
                    errors.Add($"Line {lineNumber}: A valid price is required.");
                    regularPrice = 0m;
                }
            }

            decimal? salePrice = null;
            var saleText = GetField("Sale price");
            if (!string.IsNullOrWhiteSpace(saleText))
            {
                if (decimal.TryParse(saleText, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedSale))
                {
                    salePrice = parsedSale;
                }
                else
                {
                    errors.Add($"Line {lineNumber}: Sale price is invalid.");
                }
            }

            if (regularPrice <= 0 && salePrice.HasValue)
            {
                regularPrice = salePrice.Value;
            }

            int? stockQuantity = null;
            var stockText = GetField("Stock");
            if (!string.IsNullOrWhiteSpace(stockText))
            {
                if (int.TryParse(stockText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedStock))
                {
                    stockQuantity = parsedStock;
                }
                else
                {
                    errors.Add($"Line {lineNumber}: Stock quantity is invalid.");
                }
            }

            var publishedText = GetField("Published");
            var statusText = GetField("Status");
            var published = string.Equals(publishedText, "1", StringComparison.OrdinalIgnoreCase) ||
                             string.Equals(statusText, "publish", StringComparison.OrdinalIgnoreCase);

            var categoriesRaw = GetField("Categories");
            var categories = categoriesRaw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(c => c.Replace('>', '/'))
                .Select(c => c.Trim())
                .Where(c => !string.IsNullOrWhiteSpace(c))
                .ToArray();

            var slug = GetField("Slug");
            var description = GetField("Description");
            var shortDescription = GetField("Short description");

            var row = new WooCommerceParsedRow(
                name,
                slug,
                regularPrice,
                salePrice,
                published,
                description,
                shortDescription,
                stockQuantity,
                categories,
                new List<string>());

            row.Errors.AddRange(errors);
            rows.Add(row);
        }

        return rows;
    }

    private async Task<List<WooCommerceParsedRow>> ParseJsonAsync(Stream stream, CancellationToken cancellationToken)
    {
        var rows = new List<WooCommerceParsedRow>();
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);

        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            rows.Add(new WooCommerceParsedRow(string.Empty, string.Empty, 0, null, false, null, null, null, Array.Empty<string>(), new List<string> { "JSON export must be an array of products." }));
            return rows;
        }

        foreach (var element in document.RootElement.EnumerateArray())
        {
            var errors = new List<string>();

            string name = element.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add("Product name is required.");
            }

            string slug = element.TryGetProperty("slug", out var slugElement) ? slugElement.GetString() ?? string.Empty : string.Empty;

            decimal? salePrice = null;
            if (element.TryGetProperty("sale_price", out var saleElement) && saleElement.ValueKind != JsonValueKind.Null)
            {
                if (decimal.TryParse(saleElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out var parsedSale))
                {
                    salePrice = parsedSale;
                }
                else
                {
                    errors.Add("Sale price is invalid.");
                }
            }

            var regularPrice = 0m;
            if (element.TryGetProperty("regular_price", out var regularElement) && regularElement.ValueKind != JsonValueKind.Null)
            {
                if (!decimal.TryParse(regularElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out regularPrice))
                {
                    errors.Add("Regular price is invalid.");
                }
            }
            else if (element.TryGetProperty("price", out var priceElement) && priceElement.ValueKind != JsonValueKind.Null)
            {
                if (!decimal.TryParse(priceElement.GetString(), NumberStyles.Number, CultureInfo.InvariantCulture, out regularPrice))
                {
                    errors.Add("Price is invalid.");
                }
            }
            else if (salePrice.HasValue)
            {
                regularPrice = salePrice.Value;
            }
            else
            {
                errors.Add("A price value is required.");
            }

            int? stockQuantity = null;
            if (element.TryGetProperty("stock_quantity", out var stockElement) && stockElement.ValueKind != JsonValueKind.Null)
            {
                if (stockElement.TryGetInt32(out var parsedStock))
                {
                    stockQuantity = parsedStock;
                }
                else
                {
                    errors.Add("Stock quantity is invalid.");
                }
            }

            var status = element.TryGetProperty("status", out var statusElement) ? statusElement.GetString() : null;
            var published = string.Equals(status, "publish", StringComparison.OrdinalIgnoreCase);

            var description = element.TryGetProperty("description", out var descElement) ? descElement.GetString() : null;
            var shortDescription = element.TryGetProperty("short_description", out var shortDescElement) ? shortDescElement.GetString() : null;

            var categories = element.TryGetProperty("categories", out var categoriesElement) && categoriesElement.ValueKind == JsonValueKind.Array
                ? categoriesElement.EnumerateArray()
                    .Select(c => c.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .ToArray()
                : Array.Empty<string>();

            var row = new WooCommerceParsedRow(
                name,
                slug,
                regularPrice,
                salePrice,
                published,
                description,
                shortDescription,
                stockQuantity,
                categories,
                new List<string>());

            row.Errors.AddRange(errors);
            rows.Add(row);
        }

        return rows;
    }

    private static IEnumerable<string> ParseCsvLine(string line)
    {
        if (string.IsNullOrEmpty(line))
        {
            yield break;
        }

        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (ch == ',' && !inQuotes)
            {
                yield return current.ToString().Trim();
                current.Clear();
            }
            else
            {
                current.Append(ch);
            }
        }

        yield return current.ToString().Trim();
    }

    private static string NormalizeCategorySlug(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        return Slugify(name.Replace('/', ' '));
    }

    private static string Slugify(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var builder = new StringBuilder();
        foreach (var ch in value.Trim().ToLowerInvariant())
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(ch);
            }
            else if (builder.Length == 0 || builder[^1] != '-')
            {
                builder.Append('-');
            }
        }

        return builder.ToString().Trim('-');
    }

    private static string ToTitleCase(string input)
    {
        return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(input);
    }

    private record WooCommerceParsedRow(
        string Name,
        string Slug,
        decimal RegularPrice,
        decimal? SalePrice,
        bool Published,
        string? Description,
        string? ShortDescription,
        int? StockQuantity,
        IReadOnlyList<string> Categories,
        List<string> ErrorList)
    {
        public List<string> Errors { get; } = ErrorList;
    }
}
