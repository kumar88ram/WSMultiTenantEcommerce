namespace MultiTenantEcommerce.Maui.Models;

public class Address
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public string Label { get; init; } = "Home";
    public string Recipient { get; init; } = string.Empty;
    public string Line1 { get; init; } = string.Empty;
    public string? Line2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public bool IsDefault { get; set; }
}
