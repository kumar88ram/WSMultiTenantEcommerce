using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class CheckoutService : ICheckoutService
{
    private const decimal DefaultShippingCost = 9.99m;
    private const decimal DefaultTaxRate = 0.08m;

    private readonly ApplicationDbContext _dbContext;
    private readonly ITenantResolver _tenantResolver;
    private readonly IPaymentGatewayOrchestrator _paymentGatewayOrchestrator;
    private readonly IEmailNotificationQueue _emailQueue;
    private readonly ILogger<CheckoutService> _logger;

    public CheckoutService(
        ApplicationDbContext dbContext,
        ITenantResolver tenantResolver,
        IPaymentGatewayOrchestrator paymentGatewayOrchestrator,
        IEmailNotificationQueue emailQueue,
        ILogger<CheckoutService> logger)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
        _paymentGatewayOrchestrator = paymentGatewayOrchestrator;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    public async Task<CartDto> GetOrCreateCartAsync(Guid? userId, string? guestToken, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        var cart = await FindCartAsync(tenantId, userId, guestToken, cancellationToken);
        if (cart is null)
        {
            cart = new Cart
            {
                TenantId = tenantId,
                UserId = userId,
                GuestToken = guestToken,
                ExpiresAt = guestToken is null ? null : DateTime.UtcNow.AddDays(7)
            };

            _dbContext.Carts.Add(cart);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }

        return MapCart(cart);
    }

    public async Task<CartDto> AddItemToCartAsync(AddCartItemRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Quantity), "Quantity must be greater than zero");
        }

        var tenantId = _tenantResolver.CurrentTenantId;
        var cart = await FindCartAsync(tenantId, request.UserId, request.GuestToken, cancellationToken);
        if (cart is null)
        {
            cart = new Cart
            {
                TenantId = tenantId,
                UserId = request.UserId,
                GuestToken = request.GuestToken,
                ExpiresAt = request.GuestToken is null ? null : DateTime.UtcNow.AddDays(7)
            };
            _dbContext.Carts.Add(cart);
        }

        var productQuery = _dbContext.Products
            .Include(p => p.Variants)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);
        var inventoryQuery = _dbContext.Inventories
            .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.ProductVariantId == request.ProductVariantId, cancellationToken);

        await Task.WhenAll(productQuery, inventoryQuery);

        var product = productQuery.Result ?? throw new InvalidOperationException("Product not found");
        if (!product.IsPublished)
        {
            throw new InvalidOperationException("Product is not available for sale");
        }

        ProductVariant? variant = null;
        if (request.ProductVariantId.HasValue)
        {
            variant = product.Variants.FirstOrDefault(v => v.Id == request.ProductVariantId.Value);
            if (variant is null)
            {
                throw new InvalidOperationException("Variant not found");
            }
        }

        var inventory = inventoryQuery.Result;
        if (inventory is null && request.ProductVariantId.HasValue)
        {
            inventory = await _dbContext.Inventories
                .FirstOrDefaultAsync(i => i.ProductId == request.ProductId && i.ProductVariantId == null, cancellationToken);
        }
        var availableQuantity = inventory is null ? int.MaxValue : inventory.QuantityOnHand - inventory.ReservedQuantity;
        if (availableQuantity < request.Quantity)
        {
            throw new InvalidOperationException("Insufficient inventory for the requested quantity");
        }

        var existingItem = cart.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId && i.ProductVariantId == request.ProductVariantId);

        if (existingItem is null)
        {
            existingItem = new CartItem
            {
                TenantId = tenantId,
                Cart = cart,
                ProductId = product.Id,
                Product = product,
                ProductVariantId = variant?.Id,
                ProductVariant = variant,
                Quantity = request.Quantity,
                UnitPrice = variant?.Price ?? product.Price,
                Name = variant?.Name ?? product.Name,
                Sku = variant?.Sku ?? product.Slug
            };
            cart.Items.Add(existingItem);
        }
        else
        {
            existingItem.Quantity += request.Quantity;
            existingItem.UnitPrice = variant?.Price ?? product.Price;
            existingItem.Name = variant?.Name ?? product.Name;
            existingItem.Sku = variant?.Sku ?? product.Slug;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapCart(cart);
    }

    public async Task<CheckoutResponse> CheckoutAsync(CheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
        {
            throw new ArgumentException("Email is required", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.PaymentProvider))
        {
            throw new ArgumentException("A payment provider must be supplied", nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.Currency))
        {
            throw new ArgumentException("Currency is required", nameof(request));
        }

        var tenantId = _tenantResolver.CurrentTenantId;
        var cart = await LoadCartForCheckoutAsync(tenantId, request, cancellationToken)
                   ?? throw new InvalidOperationException("Cart not found");

        if (!cart.Items.Any())
        {
            throw new InvalidOperationException("Cart is empty");
        }

        var productIds = cart.Items.Select(ci => ci.ProductId).Distinct().ToList();
        var inventories = await _dbContext.Inventories
            .Where(i => productIds.Contains(i.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var item in cart.Items)
        {
            var inventory = inventories.FirstOrDefault(i =>
                i.ProductId == item.ProductId && i.ProductVariantId == item.ProductVariantId);
            if (inventory is null)
            {
                inventory = inventories.FirstOrDefault(i => i.ProductId == item.ProductId && i.ProductVariantId == null);
            }

            if (inventory is not null)
            {
                var available = inventory.QuantityOnHand - inventory.ReservedQuantity;
                if (available < item.Quantity)
                {
                    throw new InvalidOperationException($"Insufficient stock for {item.Name}");
                }

                inventory.ReservedQuantity += item.Quantity;
            }
        }

        var subtotal = cart.Items.Sum(i => i.UnitPrice * i.Quantity);
        var discount = CalculateDiscount(subtotal, request.CouponCode);
        var taxableAmount = subtotal - discount;
        var tax = Math.Round(taxableAmount * DefaultTaxRate, 2, MidpointRounding.AwayFromZero);
        var shipping = DefaultShippingCost;
        var grandTotal = taxableAmount + tax + shipping;

        var order = new Order
        {
            TenantId = tenantId,
            UserId = request.UserId,
            GuestToken = request.GuestToken,
            OrderNumber = GenerateOrderNumber(),
            Status = OrderStatus.Pending,
            Subtotal = subtotal,
            DiscountTotal = discount,
            TaxTotal = tax,
            ShippingTotal = shipping,
            GrandTotal = grandTotal,
            Currency = request.Currency,
            Email = request.Email,
            ShippingAddress = request.ShippingAddress,
            BillingAddress = request.BillingAddress,
            CouponCode = request.CouponCode,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var item in cart.Items)
        {
            order.Items.Add(new OrderItem
            {
                TenantId = tenantId,
                Order = order,
                ProductId = item.ProductId,
                ProductVariantId = item.ProductVariantId,
                Name = item.Name,
                Sku = item.Sku,
                UnitPrice = item.UnitPrice,
                Quantity = item.Quantity,
                LineTotal = item.UnitPrice * item.Quantity
            });
        }

        var payment = new PaymentTransaction
        {
            TenantId = tenantId,
            Order = order,
            Provider = request.PaymentProvider,
            ProviderReference = Guid.NewGuid().ToString("N"),
            Amount = grandTotal,
            Currency = request.Currency,
            Status = PaymentStatus.Pending,
            RawPayload = request.PaymentMetadata is null ? null : JsonSerializer.Serialize(request.PaymentMetadata)
        };
        order.Payments.Add(payment);

        cart.IsActive = false;

        await _dbContext.Orders.AddAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var paymentIntent = await _paymentGatewayOrchestrator.PayAsync(request.PaymentProvider, order, cancellationToken);

        if (paymentIntent.Metadata.TryGetValue("providerReference", out var providerReference)
            && !string.IsNullOrWhiteSpace(providerReference))
        {
            payment.ProviderReference = providerReference;
        }

        await _emailQueue.QueueAsync(new OrderEmailNotification(
            OrderEmailNotificationType.OrderPlaced,
            order.TenantId,
            order.Email,
            order.OrderNumber,
            order.GrandTotal,
            order.Currency,
            order.CreatedAt,
            null),
            cancellationToken);

        _logger.LogInformation("Checkout completed for order {OrderNumber}", order.OrderNumber);

        return new CheckoutResponse(MapOrder(order), paymentIntent);
    }

    public async Task<OrderDto?> HandlePaymentWebhookAsync(PaymentWebhookRequest request, CancellationToken cancellationToken = default)
    {
        var payment = await _dbContext.PaymentTransactions
            .Include(p => p.Order)
            .ThenInclude(o => o.Items)
            .Include(p => p.Order)
            .ThenInclude(o => o.Payments)
            .FirstOrDefaultAsync(p => p.ProviderReference == request.ProviderReference && p.Provider == request.Provider, cancellationToken);

        if (payment is null)
        {
            _logger.LogWarning("Payment webhook received for unknown transaction {Reference}", request.ProviderReference);
            return null;
        }

        payment.Status = request.Status;
        payment.RawPayload = request.Payload;
        payment.ProcessedAt = DateTime.UtcNow;

        var order = payment.Order;
        order.UpdatedAt = DateTime.UtcNow;
        if (request.Status is PaymentStatus.Captured or PaymentStatus.Authorized)
        {
            order.Status = OrderStatus.Processing;
            order.PaidAt = DateTime.UtcNow;
        }
        else if (request.Status == PaymentStatus.Failed)
        {
            order.Status = OrderStatus.Cancelled;
        }
        else if (request.Status == PaymentStatus.Refunded)
        {
            order.Status = OrderStatus.Refunded;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<OrderListResult> GetOrdersAsync(OrderListQuery query, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var ordersQuery = _dbContext.Orders
            .AsNoTracking()
            .Where(o => o.TenantId == tenantId);

        if (query.Status.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.Status == query.Status.Value);
        }

        if (query.From.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.CreatedAt >= query.From.Value);
        }

        if (query.To.HasValue)
        {
            ordersQuery = ordersQuery.Where(o => o.CreatedAt <= query.To.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            ordersQuery = ordersQuery.Where(o => o.OrderNumber.Contains(query.Search) || o.Email.Contains(query.Search));
        }

        var totalCount = await ordersQuery.CountAsync(cancellationToken);
        var items = await ordersQuery
            .OrderByDescending(o => o.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(o.Id, o.OrderNumber, o.Status, o.GrandTotal, o.Currency, o.Email, o.CreatedAt, o.PaidAt))
            .ToListAsync(cancellationToken);

        return new OrderListResult(page, pageSize, totalCount, items);
    }

    public async Task<OrderDto?> GetOrderByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenantResolver.CurrentTenantId;
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId && o.TenantId == tenantId, cancellationToken);

        return order is null ? null : MapOrder(order);
    }

    public async Task<OrderDto> UpdateOrderStatusAsync(Guid orderId, OrderStatus status, string? trackingNumber, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found");

        order.Status = status;
        order.UpdatedAt = DateTime.UtcNow;

        if (status == OrderStatus.Shipped)
        {
            order.ShippedAt = DateTime.UtcNow;
            await _emailQueue.QueueAsync(new OrderEmailNotification(
                OrderEmailNotificationType.OrderShipped,
                order.TenantId,
                order.Email,
                order.OrderNumber,
                order.GrandTotal,
                order.Currency,
                order.CreatedAt,
                trackingNumber),
                cancellationToken);
        }
        else if (status == OrderStatus.Delivered)
        {
            order.DeliveredAt = DateTime.UtcNow;
        }
        else if (status == OrderStatus.Cancelled)
        {
            await ReleaseInventoryAsync(order, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    public async Task<OrderDto> RefundOrderAsync(Guid orderId, RefundRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Amount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.Amount));
        }

        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .Include(o => o.Payments)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found");

        var capturedPayment = order.Payments
            .OrderByDescending(p => p.ProcessedAt)
            .FirstOrDefault(p => p.Status is PaymentStatus.Captured or PaymentStatus.Authorized);

        PaymentStatus refundStatus = PaymentStatus.Refunded;
        if (capturedPayment is not null && !string.IsNullOrWhiteSpace(capturedPayment.Provider))
        {
            var refundRequestModel = new PaymentRefundRequest(
                capturedPayment.ProviderReference,
                request.Amount,
                order.Currency,
                request.Reason);

            try
            {
                refundStatus = await _paymentGatewayOrchestrator.RefundAsync(
                    capturedPayment.Provider,
                    refundRequestModel,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Failed to execute refund via provider {Provider} for transaction {Reference}",
                    capturedPayment.Provider,
                    capturedPayment.ProviderReference);
            }
        }

        var refund = new PaymentTransaction
        {
            TenantId = order.TenantId,
            Order = order,
            Provider = capturedPayment?.Provider ?? "manual",
            ProviderReference = $"refund_{Guid.NewGuid():N}",
            Amount = -Math.Abs(request.Amount),
            Currency = order.Currency,
            Status = refundStatus,
            RawPayload = request.Reason
        };

        order.Payments.Add(refund);
        order.Status = OrderStatus.Refunded;
        order.UpdatedAt = DateTime.UtcNow;

        await ReleaseInventoryAsync(order, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return MapOrder(order);
    }

    private async Task<Cart?> FindCartAsync(Guid tenantId, Guid? userId, string? guestToken, CancellationToken cancellationToken)
    {
        if (userId is null && string.IsNullOrWhiteSpace(guestToken))
        {
            throw new InvalidOperationException("A userId or guest token must be provided");
        }

        var query = _dbContext.Carts
            .Include(c => c.Items)
            .Where(c => c.TenantId == tenantId && c.IsActive);

        if (userId.HasValue)
        {
            query = query.Where(c => c.UserId == userId);
        }
        else if (!string.IsNullOrWhiteSpace(guestToken))
        {
            query = query.Where(c => c.GuestToken == guestToken);
        }

        return await query.FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<Cart?> LoadCartForCheckoutAsync(Guid tenantId, CheckoutRequest request, CancellationToken cancellationToken)
    {
        Cart? cart = null;
        if (request.CartId.HasValue)
        {
            cart = await _dbContext.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.Id == request.CartId.Value && c.TenantId == tenantId && c.IsActive, cancellationToken);
        }

        if (cart is null)
        {
            cart = await FindCartAsync(tenantId, request.UserId, request.GuestToken, cancellationToken);
        }

        return cart;
    }

    private static decimal CalculateDiscount(decimal subtotal, string? couponCode)
    {
        if (string.IsNullOrWhiteSpace(couponCode))
        {
            return 0m;
        }

        return couponCode.Equals("SAVE10", StringComparison.OrdinalIgnoreCase)
            ? Math.Round(subtotal * 0.10m, 2, MidpointRounding.AwayFromZero)
            : 0m;
    }

    private async Task ReleaseInventoryAsync(Order order, CancellationToken cancellationToken)
    {
        var inventories = await _dbContext.Inventories
            .Where(i => order.Items.Select(oi => oi.ProductId).Contains(i.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var item in order.Items)
        {
            var inventory = inventories.FirstOrDefault(i =>
                i.ProductId == item.ProductId && i.ProductVariantId == item.ProductVariantId);

            if (inventory is null)
            {
                continue;
            }

            inventory.ReservedQuantity = Math.Max(0, inventory.ReservedQuantity - item.Quantity);
        }
    }

    private static CartDto MapCart(Cart cart)
    {
        var items = cart.Items
            .Select(item => new CartItemDto(
                item.Id,
                item.ProductId,
                item.ProductVariantId,
                item.Name,
                item.Sku,
                item.UnitPrice,
                item.Quantity,
                item.UnitPrice * item.Quantity))
            .ToList();

        var subtotal = items.Sum(i => i.LineTotal);

        return new CartDto(
            cart.Id,
            cart.TenantId,
            cart.UserId,
            cart.GuestToken,
            cart.IsActive,
            cart.CreatedAt,
            cart.ExpiresAt,
            items,
            subtotal);
    }

    private static OrderDto MapOrder(Order order)
    {
        var items = order.Items
            .Select(item => new OrderItemDto(
                item.Id,
                item.ProductId,
                item.ProductVariantId,
                item.Name,
                item.Sku,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal))
            .ToList();

        var payments = order.Payments
            .Select(payment => new PaymentTransactionDto(
                payment.Id,
                payment.Provider,
                payment.ProviderReference,
                payment.Amount,
                payment.Currency,
                payment.Status,
                payment.ProcessedAt))
            .ToList();

        return new OrderDto(
            order.Id,
            order.OrderNumber,
            order.Status,
            order.Subtotal,
            order.DiscountTotal,
            order.TaxTotal,
            order.ShippingTotal,
            order.GrandTotal,
            order.Currency,
            order.Email,
            order.ShippingAddress,
            order.BillingAddress,
            order.CouponCode,
            order.CreatedAt,
            order.PaidAt,
            order.ShippedAt,
            order.DeliveredAt,
            items,
            payments);
    }

    private static string GenerateOrderNumber() => $"ORD-{DateTime.UtcNow:yyyyMMddHHmmss}-{Random.Shared.Next(1000, 9999)}";
}
