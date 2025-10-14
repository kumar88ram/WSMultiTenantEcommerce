using System.Xml.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Presentation.Controllers;

[ApiController]
[Route("sitemap.xml")]
public class SitemapController : ControllerBase
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;

    public SitemapController(ApplicationDbContext dbContext, ITenantResolver tenantResolver)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
    }

    [HttpGet]
    [AllowAnonymous]
    [Produces("application/xml")]
    public async Task<IActionResult> GetSitemap(CancellationToken cancellationToken)
    {
        if (_tenantResolver.CurrentTenantId == Guid.Empty)
        {
            return BadRequest(new { error = "Tenant context is required to generate a sitemap." });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host.ToUriComponent()}";
        var sitemapNamespace = XNamespace.Get("http://www.sitemaps.org/schemas/sitemap/0.9");
        var urls = new List<XElement>
        {
            CreateUrlElement(sitemapNamespace, baseUrl, DateTime.UtcNow)
        };

        var categories = await _dbContext.Categories
            .AsNoTracking()
            .Select(category => new
            {
                category.Slug,
                category.CreatedAt,
                category.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var category in categories)
        {
            var categoryUrl = $"{baseUrl}/collections/{category.Slug}";
            urls.Add(CreateUrlElement(sitemapNamespace, categoryUrl, category.UpdatedAt ?? category.CreatedAt));
        }

        var products = await _dbContext.Products
            .AsNoTracking()
            .Where(product => product.IsPublished)
            .Select(product => new
            {
                product.Slug,
                product.CreatedAt,
                product.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        foreach (var product in products)
        {
            var productUrl = $"{baseUrl}/products/{product.Slug}";
            urls.Add(CreateUrlElement(sitemapNamespace, productUrl, product.UpdatedAt ?? product.CreatedAt));
        }

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", "yes"),
            new XElement(sitemapNamespace + "urlset", urls));

        return Content(document.ToString(SaveOptions.DisableFormatting), "application/xml");
    }

    private static XElement CreateUrlElement(XNamespace sitemapNamespace, string url, DateTime? lastModified)
    {
        var element = new XElement(
            sitemapNamespace + "url",
            new XElement(sitemapNamespace + "loc", url));

        if (lastModified.HasValue)
        {
            element.Add(new XElement(sitemapNamespace + "lastmod", lastModified.Value.ToUniversalTime().ToString("yyyy-MM-dd")));
        }

        return element;
    }
}
