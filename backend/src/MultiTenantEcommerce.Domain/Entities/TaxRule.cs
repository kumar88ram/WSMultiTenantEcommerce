namespace MultiTenantEcommerce.Domain.Entities;

public class TaxRule : BaseEntity
{
    public string CountryCode { get; set; } = string.Empty;
    public string? StateCode { get; set; }
    public TaxCalculationType CalculationType { get; set; } = TaxCalculationType.Percentage;
    public decimal Rate { get; set; }
    public bool AppliesToShipping { get; set; }
    public bool IsDefault { get; set; }
}
