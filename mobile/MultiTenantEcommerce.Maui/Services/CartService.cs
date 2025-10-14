using System.Collections.ObjectModel;
using MultiTenantEcommerce.Maui.Models;
using System.Linq;

namespace MultiTenantEcommerce.Maui.Services;

public class CartService
{
    public ObservableCollection<CartItem> Items { get; } = new();

    public event EventHandler? CartChanged;

    public CartService()
    {
        Items.CollectionChanged += (_, _) => CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public void AddToCart(Product product, ProductVariant? variant, int quantity)
    {
        var price = product.Price + (variant?.PriceModifier ?? 0);
        var existing = Items.FirstOrDefault(i => i.Product.Id == product.Id && (i.Variant?.SelectedOption ?? string.Empty) == (variant?.SelectedOption ?? string.Empty));
        if (existing is not null)
        {
            existing.Quantity += quantity;
            existing.UnitPrice = price;
            CartChanged?.Invoke(this, EventArgs.Empty);
            return;
        }

        Items.Add(new CartItem
        {
            Product = product,
            Variant = variant,
            Quantity = quantity,
            UnitPrice = price
        });
    }

    public void RemoveFromCart(CartItem item)
    {
        Items.Remove(item);
        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Clear()
    {
        Items.Clear();
        CartChanged?.Invoke(this, EventArgs.Empty);
    }

    public decimal Subtotal => Items.Sum(i => i.TotalPrice);

    public decimal Taxes => Math.Round(Subtotal * 0.07m, 2);

    public decimal Total(decimal shippingFee) => Subtotal + Taxes + shippingFee;
}
