using System.Text.Json;

namespace MultiTenantEcommerce.Application.Models;

public record SeoMetaDto(string? MetaTitle, string? MetaDescription, string? MetaKeywords);

public record StoreSettingsDto(
    Guid Id,
    string? LogoUrl,
    string Title,
    string Currency,
    string Timezone,
    SeoMetaDto SeoMeta);

public record UpdateStoreSettingsRequest(
    string? LogoUrl,
    string Title,
    string Currency,
    string Timezone,
    SeoMetaDto SeoMeta);

public record ThemeDto(
    Guid Id,
    string Name,
    string DisplayName,
    string? Description,
    string? PreviewImageUrl,
    bool IsActive,
    DateTime? AppliedAt);

public record CreateThemeRequest(
    string Name,
    string DisplayName,
    string? Description,
    string? PreviewImageUrl);

public record CategoryDto(Guid? Id, string Name, string Slug, string? Description);

public record ProductAttributeDto(Guid? Id, string Name, string Value);

public record ProductVariantDto(
    Guid? Id,
    string Name,
    string? Sku,
    decimal Price,
    int StockQuantity,
    IDictionary<string, string>? Options);

public record ProductImageDto(Guid? Id, string Url, string? AltText, int SortOrder);

public record ProductDto(
    Guid Id,
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? CompareAtPrice,
    int InventoryQuantity,
    bool IsPublished,
    IEnumerable<CategoryDto> Categories,
    IEnumerable<ProductAttributeDto> Attributes,
    IEnumerable<ProductVariantDto> Variants,
    IEnumerable<ProductImageDto> Images);

public record CreateProductRequest(
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? CompareAtPrice,
    int InventoryQuantity,
    bool IsPublished,
    IEnumerable<CategoryDto> Categories,
    IEnumerable<ProductAttributeDto> Attributes,
    IEnumerable<ProductVariantDto> Variants,
    IEnumerable<ProductImageDto> Images);

public record UpdateProductRequest(
    string Name,
    string Slug,
    string? Description,
    decimal Price,
    decimal? CompareAtPrice,
    int InventoryQuantity,
    bool IsPublished,
    IEnumerable<CategoryDto> Categories,
    IEnumerable<ProductAttributeDto> Attributes,
    IEnumerable<ProductVariantDto> Variants,
    IEnumerable<ProductImageDto> Images);

public record MenuDefinitionDto(Guid Id, string Name, JsonElement Tree);

public record UpsertMenuDefinitionRequest(string Name, JsonElement Tree);

public record FormFieldDto(
    Guid? Id,
    string Label,
    string Name,
    string Type,
    bool IsRequired,
    string? Placeholder,
    IEnumerable<string>? Options);

public record FormDefinitionDto(
    Guid Id,
    string Name,
    string? Description,
    IEnumerable<FormFieldDto> Fields);

public record SaveFormDefinitionRequest(
    string Name,
    string? Description,
    IEnumerable<FormFieldDto> Fields);

public record WidgetDefinitionDto(Guid Id, string Name, string Type, JsonElement Config);

public record SaveWidgetDefinitionRequest(string Name, string Type, JsonElement Config);

public static class MenuBuilderSamples
{
    public const string SampleMenuJson = """
{
  "items": [
    {
      "title": "Home",
      "url": "/",
      "children": []
    },
    {
      "title": "Shop",
      "url": "/shop",
      "children": [
        {
          "title": "New Arrivals",
          "url": "/shop/new",
          "children": []
        },
        {
          "title": "Sale",
          "url": "/shop/sale",
          "children": []
        }
      ]
    }
  ]
}
""";
}

public static class FormBuilderSamples
{
    public const string SampleFormJson = """
{
  "name": "Contact Us",
  "description": "Capture customer inquiries",
  "fields": [
    {
      "label": "Full Name",
      "name": "fullName",
      "type": "text",
      "required": true,
      "placeholder": "Jane Doe"
    },
    {
      "label": "Email",
      "name": "email",
      "type": "email",
      "required": true,
      "placeholder": "jane@example.com"
    },
    {
      "label": "Order Number",
      "name": "orderNumber",
      "type": "number",
      "required": false
    },
    {
      "label": "Inquiry Type",
      "name": "inquiryType",
      "type": "select",
      "required": true,
      "options": ["General", "Shipping", "Returns"]
    },
    {
      "label": "Attachment",
      "name": "attachment",
      "type": "file",
      "required": false
    }
  ]
}
""";
}

public static class WidgetSamples
{
    public const string SampleWidgetJson = """
{
  "type": "hero",
  "name": "HomepageHero",
  "settings": {
    "headline": "Welcome to Your Store",
    "subheadline": "Discover curated products tailored for you.",
    "backgroundImage": "https://cdn.example.com/hero.jpg",
    "cta": {
      "label": "Shop Now",
      "url": "/shop"
    }
  }
}
""";
}
