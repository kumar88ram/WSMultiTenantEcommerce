using System.Text;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Constants;
using MultiTenantEcommerce.Application.Models.Pos;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class PointOfSaleService : IPointOfSaleService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;
    private readonly IPluginManagerService _pluginManagerService;

    public PointOfSaleService(
        ApplicationDbContext dbContext,
        ITenantResolver tenantResolver,
        IPluginManagerService pluginManagerService)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
        _pluginManagerService = pluginManagerService;
    }

    public async Task<PosSaleResponse> CreateOfflineSaleAsync(PosSaleRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Items is null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("At least one line item is required to create a POS sale.");
        }

        var tenantId = _tenantResolver.CurrentTenantId;
        await _pluginManagerService.EnsurePluginEnabledAsync(tenantId, PluginSystemKeys.Pos, cancellationToken);

        foreach (var item in request.Items)
        {
            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException("Line item quantities must be greater than zero.");
            }

            if (item.UnitPrice < 0)
            {
                throw new InvalidOperationException("Line item prices cannot be negative.");
            }
        }

        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var subtotal = 0m;
            var lineDiscountTotal = 0m;
            var order = new Order
            {
                TenantId = tenantId,
                OrderNumber = GenerateOrderNumber(),
                Status = OrderStatus.Processing,
                Currency = string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency,
                TaxTotal = request.TaxTotal,
                ShippingTotal = 0,
                Email = request.CustomerEmail ?? string.Empty,
                BillingAddress = string.Empty,
                ShippingAddress = string.Empty,
                PaidAt = request.AmountPaid > 0 ? request.OccurredAtUtc ?? DateTime.UtcNow : null,
                CreatedAt = request.OccurredAtUtc ?? DateTime.UtcNow
            };

            foreach (var line in request.Items)
            {
                var product = await _dbContext.Products
                    .FirstOrDefaultAsync(p => p.Id == line.ProductId, cancellationToken)
                    ?? throw new InvalidOperationException($"Product '{line.ProductId}' could not be found.");

                ProductVariant? variant = null;
                if (line.ProductVariantId.HasValue)
                {
                    variant = await _dbContext.ProductVariants
                        .FirstOrDefaultAsync(v => v.Id == line.ProductVariantId.Value && v.ProductId == product.Id, cancellationToken)
                        ?? throw new InvalidOperationException($"Variant '{line.ProductVariantId}' was not found for product '{product.Id}'.");
                }

                var inventory = await ResolveInventoryAsync(line, cancellationToken);

                if (inventory.QuantityOnHand < line.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient inventory for product '{product.Name}'.");
                }

                inventory.QuantityOnHand -= line.Quantity;
                inventory.LastAdjustedAt = DateTime.UtcNow;

                var lineSubtotal = line.UnitPrice * line.Quantity;
                subtotal += lineSubtotal;

                var discountAmount = Math.Max(0, line.DiscountAmount ?? 0);
                if (discountAmount > lineSubtotal)
                {
                    discountAmount = lineSubtotal;
                }
                lineDiscountTotal += discountAmount;
                var lineTotal = lineSubtotal - discountAmount;

                var orderItem = new OrderItem
                {
                    TenantId = tenantId,
                    Order = order,
                    Product = product,
                    ProductId = product.Id,
                    ProductVariant = variant,
                    ProductVariantId = variant?.Id,
                    Name = line.OverrideName ?? variant?.Name ?? product.Name,
                    Sku = line.OverrideSku ?? variant?.Sku,
                    UnitPrice = line.UnitPrice,
                    Quantity = line.Quantity,
                    LineTotal = lineTotal
                };

                order.Items.Add(orderItem);
            }

            var discountTotal = request.DiscountTotal ?? lineDiscountTotal;
            var grandTotal = subtotal - discountTotal + request.TaxTotal;
            if (grandTotal < 0)
            {
                throw new InvalidOperationException("The calculated total for the sale cannot be negative.");
            }

            if (request.AmountPaid > 0 && request.AmountPaid < grandTotal)
            {
                throw new InvalidOperationException("The amount paid cannot be less than the order total.");
            }

            order.Subtotal = subtotal;
            order.DiscountTotal = discountTotal;
            order.GrandTotal = grandTotal;

            if (request.AmountPaid > 0)
            {
                var payment = new PaymentTransaction
                {
                    TenantId = tenantId,
                    Order = order,
                    Amount = request.AmountPaid,
                    Currency = order.Currency,
                    Provider = "POS",
                    ProviderReference = request.PaymentReference ?? string.Empty,
                    Status = PaymentStatus.Captured,
                    ProcessedAt = request.OccurredAtUtc ?? DateTime.UtcNow
                };

                order.Payments.Add(payment);
            }

            await _dbContext.Orders.AddAsync(order, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return new PosSaleResponse(
                order.Id,
                order.OrderNumber,
                order.Subtotal,
                order.DiscountTotal,
                order.TaxTotal,
                order.GrandTotal,
                order.CreatedAt,
                order.PaidAt,
                order.Currency,
                request.PaymentMethod,
                request.PaymentReference);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<InventorySyncResult>> SyncInventoryAsync(IEnumerable<InventorySyncRequest> requests, CancellationToken cancellationToken = default)
    {
        if (requests is null)
        {
            throw new InvalidOperationException("Inventory sync payload cannot be null.");
        }

        var tenantId = _tenantResolver.CurrentTenantId;
        await _pluginManagerService.EnsurePluginEnabledAsync(tenantId, PluginSystemKeys.Pos, cancellationToken);

        var results = new List<InventorySyncResult>();

        foreach (var request in requests)
        {
            try
            {
                var inventory = await ResolveInventoryAsync(new PosSaleItemRequest
                {
                    ProductId = request.ProductId,
                    ProductVariantId = request.ProductVariantId,
                    Quantity = 0,
                    UnitPrice = 0
                }, cancellationToken, track: true);

                inventory.QuantityOnHand = request.QuantityOnHand;
                inventory.LastAdjustedAt = request.SyncedAtUtc ?? DateTime.UtcNow;

                results.Add(new InventorySyncResult(request.ProductId, request.ProductVariantId, true, null));
            }
            catch (Exception ex)
            {
                results.Add(new InventorySyncResult(request.ProductId, request.ProductVariantId, false, ex.Message));
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return results;
    }

    public async Task<ReceiptResponse> GenerateReceiptAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        await _pluginManagerService.EnsurePluginEnabledAsync(tenantId, PluginSystemKeys.Pos, cancellationToken);

        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new KeyNotFoundException("The requested sale could not be found.");

        var builder = new StringBuilder();
        builder.AppendLine("*** POS RECEIPT ***");
        builder.AppendLine($"Sale: {order.OrderNumber}");
        builder.AppendLine($"Date: {order.CreatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine(new string('-', 32));

        foreach (var item in order.Items)
        {
            builder.AppendLine(item.Name);
            builder.AppendLine($"  {item.Quantity} x {item.UnitPrice:C2} = {item.LineTotal:C2}");
        }

        builder.AppendLine(new string('-', 32));
        builder.AppendLine($"Subtotal: {order.Subtotal:C2}");
        builder.AppendLine($"Discounts: {order.DiscountTotal:C2}");
        builder.AppendLine($"Tax: {order.TaxTotal:C2}");
        builder.AppendLine($"Grand Total: {order.GrandTotal:C2}");

        if (order.PaidAt.HasValue)
        {
            builder.AppendLine($"Paid At: {order.PaidAt:yyyy-MM-dd HH:mm:ss} UTC");
        }

        return new ReceiptResponse(order.Id, order.OrderNumber, "text/plain", builder.ToString());
    }

    private async Task<Inventory> ResolveInventoryAsync(PosSaleItemRequest item, CancellationToken cancellationToken, bool track = true)
    {
        var query = track ? _dbContext.Inventories : _dbContext.Inventories.AsNoTracking();

        Inventory? inventory;
        if (item.ProductVariantId.HasValue)
        {
            inventory = await query.FirstOrDefaultAsync(i => i.ProductVariantId == item.ProductVariantId.Value, cancellationToken);
        }
        else
        {
            inventory = await query.FirstOrDefaultAsync(i => i.ProductId == item.ProductId && i.ProductVariantId == null, cancellationToken);
        }

        if (inventory is null)
        {
            throw new InvalidOperationException("No inventory record was found for the provided item.");
        }

        return inventory;
    }

    private static string GenerateOrderNumber()
    {
        return $"POS-{DateTime.UtcNow:yyyyMMddHHmmssfff}-{Random.Shared.Next(100, 999)}";
    }
}
