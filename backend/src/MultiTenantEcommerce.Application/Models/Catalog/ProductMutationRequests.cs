using System.ComponentModel.DataAnnotations;

namespace MultiTenantEcommerce.Application.Models.Catalog;

public class CreateProductVariantRequest
{
    [Required]
    [MaxLength(150)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Sku { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal? CompareAtPrice { get; set; }

    public bool IsActive { get; set; } = true;

    [Required]
    public IDictionary<string, string> AttributeSelections { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [Range(0, int.MaxValue)]
    public int QuantityOnHand { get; set; }

    [Range(0, int.MaxValue)]
    public int ReservedQuantity { get; set; }
}

public class UpdateInventoryRequest
{
    [Range(0, int.MaxValue)]
    public int QuantityOnHand { get; set; }

    [Range(0, int.MaxValue)]
    public int ReservedQuantity { get; set; }
}
