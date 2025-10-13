using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Catalog;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("api/products")]
public class ProductCatalogController : ControllerBase
{
    private readonly IProductCatalogService _productCatalogService;

    public ProductCatalogController(IProductCatalogService productCatalogService)
    {
        _productCatalogService = productCatalogService;
    }

    [HttpGet]
    [AllowAnonymous]
    public async Task<ActionResult<ProductListResult>> GetProducts([FromQuery] ProductQueryParameters parameters, CancellationToken cancellationToken)
    {
        var attributeFilters = ParseAttributeFilters(parameters.AttributeFilters);
        var query = new ProductListQuery(
            parameters.Page,
            parameters.PageSize,
            parameters.CategorySlug,
            parameters.MinPrice,
            parameters.MaxPrice,
            attributeFilters);

        var result = await _productCatalogService.GetProductsAsync(query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("{slug}")]
    [AllowAnonymous]
    public async Task<ActionResult<ProductDetailDto>> GetProductDetail(string slug, CancellationToken cancellationToken)
    {
        var product = await _productCatalogService.GetProductBySlugAsync(slug, cancellationToken);
        if (product is null)
        {
            return NotFound();
        }

        return Ok(product);
    }

    [HttpPost("{productId:guid}/variants")]
    [Authorize]
    public async Task<ActionResult<ProductVariantDto>> CreateVariant(Guid productId, [FromBody] CreateProductVariantRequest request, CancellationToken cancellationToken)
    {
        var variant = await _productCatalogService.CreateVariantAsync(productId, request, cancellationToken);
        return Ok(variant);
    }

    [HttpPut("variants/{variantId:guid}/inventory")]
    [Authorize]
    public async Task<ActionResult<InventoryDto>> UpdateInventory(Guid variantId, [FromBody] UpdateInventoryRequest request, CancellationToken cancellationToken)
    {
        var inventory = await _productCatalogService.UpdateInventoryAsync(variantId, request, cancellationToken);
        return Ok(inventory);
    }

    [HttpPost("import")]
    [Authorize]
    public async Task<ActionResult<BulkImportResult>> ImportProducts([FromForm] ProductImportRequest request, CancellationToken cancellationToken)
    {
        if (request.File is null || request.File.Length == 0)
        {
            return BadRequest("A CSV file must be provided.");
        }

        await using var stream = request.File.OpenReadStream();
        var result = await _productCatalogService.ImportProductsFromCsvAsync(stream, request.UseStoredProcedure, cancellationToken);
        return Ok(result);
    }

    private static IReadOnlyDictionary<string, string[]> ParseAttributeFilters(string? rawFilters)
    {
        if (string.IsNullOrWhiteSpace(rawFilters))
        {
            return new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        }

        var filters = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        var pairs = rawFilters.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var parts = pair.Split('=', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            var values = parts[1].Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            filters[parts[0]] = values;
        }

        return filters;
    }

    public class ProductQueryParameters
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? CategorySlug { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? AttributeFilters { get; set; }
    }

    public class ProductImportRequest
    {
        [FromForm(Name = "file")]
        public IFormFile? File { get; set; }

        [FromForm(Name = "useStoredProcedure")]
        public bool UseStoredProcedure { get; set; }
    }
}
