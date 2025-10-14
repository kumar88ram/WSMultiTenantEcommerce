namespace MultiTenantEcommerce.Maui.Models;

public class UserProfile
{
    public string FullName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string LoyaltyLevel { get; init; } = "Bronze";
    public int LoyaltyPoints { get; init; }
    public IReadOnlyList<Address> Addresses { get; init; } = Array.Empty<Address>();
}
