namespace MultiTenantEcommerce.Maui.Models;

public class HomeFeed
{
    public IReadOnlyList<Product> FeaturedProducts { get; init; } = Array.Empty<Product>();
    public IReadOnlyList<Category> Categories { get; init; } = Array.Empty<Category>();
    public IReadOnlyList<CampaignBanner> Campaigns { get; init; } = Array.Empty<CampaignBanner>();
}
