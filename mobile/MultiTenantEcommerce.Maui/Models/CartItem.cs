using CommunityToolkit.Mvvm.ComponentModel;

namespace MultiTenantEcommerce.Maui.Models;

public partial class CartItem : ObservableObject
{
    [ObservableProperty]
    private Product _product = new();

    [ObservableProperty]
    private ProductVariant? _variant;

    [ObservableProperty]
    private int _quantity = 1;

    [ObservableProperty]
    private decimal _unitPrice;

    public decimal TotalPrice => Quantity * UnitPrice;

    partial void OnQuantityChanged(int value) => OnPropertyChanged(nameof(TotalPrice));

    partial void OnUnitPriceChanged(decimal value) => OnPropertyChanged(nameof(TotalPrice));
}
