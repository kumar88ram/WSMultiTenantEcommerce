using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.Controllers.StoreAdmin;

[ApiController]
[Route("store-admin/products")]
[Authorize(Roles = "StoreAdmin")]
public class ProductsController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public ProductsController(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<ProductDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts(CancellationToken cancellationToken)
    {
        var products = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Attributes)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        return Ok(products.Select(MapToDto));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .AsNoTracking()
            .Include(p => p.ProductCategories)
                .ThenInclude(pc => pc.Category)
            .Include(p => p.Attributes)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        return Ok(MapToDto(product));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        var product = new Product
        {
            TenantId = tenantId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            Price = request.Price,
            CompareAtPrice = request.CompareAtPrice,
            InventoryQuantity = request.InventoryQuantity,
            IsPublished = request.IsPublished,
            CreatedAt = DateTime.UtcNow
        };

        var productCategories = await ResolveCategoriesAsync(request.Categories, cancellationToken);
        foreach (var productCategory in productCategories)
        {
            productCategory.ProductId = product.Id;
            productCategory.Product = product;
            product.ProductCategories.Add(productCategory);
        }

        product.Attributes = MapAttributes(request.Attributes, product.Id, tenantId).ToList();
        product.Variants = MapVariants(request.Variants, product.Id, tenantId).ToList();
        product.Images = MapImages(request.Images, product.Id, tenantId).ToList();

        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, MapToDto(product));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductRequest request, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.Attributes)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        product.Name = request.Name;
        product.Slug = request.Slug;
        product.Description = request.Description;
        product.Price = request.Price;
        product.CompareAtPrice = request.CompareAtPrice;
        product.InventoryQuantity = request.InventoryQuantity;
        product.IsPublished = request.IsPublished;
        product.UpdatedAt = DateTime.UtcNow;

        _dbContext.ProductCategories.RemoveRange(product.ProductCategories);
        product.ProductCategories.Clear();
        var categories = await ResolveCategoriesAsync(request.Categories, cancellationToken);
        foreach (var productCategory in categories)
        {
            productCategory.ProductId = product.Id;
            productCategory.Product = product;
            product.ProductCategories.Add(productCategory);
        }

        _dbContext.ProductAttributes.RemoveRange(product.Attributes);
        product.Attributes = MapAttributes(request.Attributes, product.Id, product.TenantId).ToList();

        _dbContext.ProductVariants.RemoveRange(product.Variants);
        product.Variants = MapVariants(request.Variants, product.Id, product.TenantId).ToList();

        _dbContext.ProductImages.RemoveRange(product.Images);
        product.Images = MapImages(request.Images, product.Id, product.TenantId).ToList();

        await _dbContext.SaveChangesAsync(cancellationToken);

        return Ok(MapToDto(product));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await _dbContext.Products
            .Include(p => p.ProductCategories)
            .Include(p => p.Attributes)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
        {
            return NotFound();
        }

        _dbContext.ProductCategories.RemoveRange(product.ProductCategories);
        _dbContext.ProductAttributes.RemoveRange(product.Attributes);
        _dbContext.ProductVariants.RemoveRange(product.Variants);
        _dbContext.ProductImages.RemoveRange(product.Images);
        _dbContext.Products.Remove(product);

        await _dbContext.SaveChangesAsync(cancellationToken);

        return NoContent();
    }

    private async Task<List<ProductCategory>> ResolveCategoriesAsync(IEnumerable<CategoryDto> categoryDtos, CancellationToken cancellationToken)
    {
        var result = new List<ProductCategory>();
        if (categoryDtos is null)
        {
            return result;
        }

        foreach (var categoryDto in categoryDtos)
        {
            Category category;
            if (categoryDto.Id.HasValue)
            {
                category = await _dbContext.Categories.FirstOrDefaultAsync(c => c.Id == categoryDto.Id.Value, cancellationToken)
                    ?? throw new ArgumentException($"Category with id {categoryDto.Id} was not found.");

                category.Name = categoryDto.Name;
                category.Slug = categoryDto.Slug;
                category.Description = categoryDto.Description;
                category.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                category = new Category
                {
                    TenantId = _tenantResolver.CurrentTenantId,
                    Name = categoryDto.Name,
                    Slug = categoryDto.Slug,
                    Description = categoryDto.Description,
                    CreatedAt = DateTime.UtcNow
                };

                _dbContext.Categories.Add(category);
            }

            result.Add(new ProductCategory
            {
                Category = category,
                CategoryId = category.Id,
                TenantId = _tenantResolver.CurrentTenantId
            });
        }

        return result;
    }

    private static IEnumerable<ProductAttribute> MapAttributes(IEnumerable<ProductAttributeDto> attributeDtos, Guid productId, Guid tenantId)
    {
        if (attributeDtos is null)
        {
            yield break;
        }

        foreach (var attribute in attributeDtos)
        {
            yield return new ProductAttribute
            {
                TenantId = tenantId,
                ProductId = productId,
                Name = attribute.Name,
                Value = attribute.Value,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    private static IEnumerable<ProductVariant> MapVariants(IEnumerable<ProductVariantDto> variantDtos, Guid productId, Guid tenantId)
    {
        if (variantDtos is null)
        {
            yield break;
        }

        foreach (var variant in variantDtos)
        {
            yield return new ProductVariant
            {
                TenantId = tenantId,
                ProductId = productId,
                Name = variant.Name,
                Sku = variant.Sku,
                Price = variant.Price,
                StockQuantity = variant.StockQuantity,
                OptionValuesJson = variant.Options is null ? null : JsonSerializer.Serialize(variant.Options),
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    private static IEnumerable<ProductImage> MapImages(IEnumerable<ProductImageDto> imageDtos, Guid productId, Guid tenantId)
    {
        if (imageDtos is null)
        {
            yield break;
        }

        foreach (var image in imageDtos)
        {
            yield return new ProductImage
            {
                TenantId = tenantId,
                ProductId = productId,
                Url = image.Url,
                AltText = image.AltText,
                SortOrder = image.SortOrder,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    private static ProductDto MapToDto(Product product)
    {
        var categories = product.ProductCategories
            .Select(pc => pc.Category)
            .Where(category => category is not null)
            .Select(category => new CategoryDto(category!.Id, category.Name, category.Slug, category.Description))
            .ToList();

        var attributes = product.Attributes
            .Select(attribute => new ProductAttributeDto(attribute.Id, attribute.Name, attribute.Value))
            .ToList();

        var variants = product.Variants
            .Select(variant => new ProductVariantDto(
                variant.Id,
                variant.Name,
                variant.Sku,
                variant.Price,
                variant.StockQuantity,
                string.IsNullOrWhiteSpace(variant.OptionValuesJson)
                    ? null
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(variant.OptionValuesJson)))
            .ToList();

        var images = product.Images
            .OrderBy(image => image.SortOrder)
            .Select(image => new ProductImageDto(image.Id, image.Url, image.AltText, image.SortOrder))
            .ToList();

        return new ProductDto(
            product.Id,
            product.Name,
            product.Slug,
            product.Description,
            product.Price,
            product.CompareAtPrice,
            product.InventoryQuantity,
            product.IsPublished,
            categories,
            attributes,
            variants,
            images);
    }
}
