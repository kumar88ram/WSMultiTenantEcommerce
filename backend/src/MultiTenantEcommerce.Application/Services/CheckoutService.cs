using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;
using MultiTenantEcommerce.Application.Models.Promotions;
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
    private readonly IPromotionEngine _promotionEngine;

    public CheckoutService(
        ApplicationDbContext dbContext,
        ITenantResolver tenantResolver,
        IPaymentGatewayOrchestrator paymentGatewayOrchestrator,
        IEmailNotificationQueue emailQueue,
        IPromotionEngine promotionEngine,
        ILogger<CheckoutService> logger)
    {
        _dbContext = dbContext;
        _tenantResolver = tenantResolver;
        _paymentGatewayOrchestrator = paymentGatewayOrchestrator;
        _emailQueue = emailQueue;
        _promotionEngine = promotionEngine;
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

    public async Task<CheckoutConfigurationDto> GetCheckoutConfigurationAsync(CancellationToken cancellationToken = default)
    {
        var currency = await ResolveStoreCurrencyAsync(cancellationToken) ?? "USD";

        var shippingMethods = new List<CheckoutShippingMethodDto>
        {
            new(
                "standard",
                "Standard shipping",
                9.99m,
                currency,
                "Delivered within 4-6 business days",
                4,
                6),
            new(
                "express",
                "Express shipping",
                19.99m,
                currency,
                "Delivered within 1-2 business days",
                1,
                2)
        };

        var paymentMethods = new List<CheckoutPaymentMethodDto>
        {
            new(
                "stripe_card",
                "Credit or debit card",
                "stripe",
                "inline_card",
                "Securely enter your card details without leaving the checkout.",
                new Dictionary<string, string>
                {
                    ["publishableKey"] = "pk_test_inline_card_placeholder"
                }),
            new(
                "stripe_hosted",
                "Stripe Checkout",
                "stripe",
                "hosted_redirect",
                "Complete your payment on the hosted Stripe checkout page.",
                null)
        };

        return new CheckoutConfigurationDto(shippingMethods, paymentMethods);
    }

    public async Task<CheckoutSessionDto> CreateCheckoutSessionAsync(CreateCheckoutSessionRequestDto request, CancellationToken cancellationToken = default)
    {
        var configuration = await GetCheckoutConfigurationAsync(cancellationToken);
        var shippingMethod = configuration.ShippingMethods.FirstOrDefault(m => string.Equals(m.Id, request.ShippingMethodId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("The selected shipping method is not available.");
        var paymentMethod = configuration.PaymentMethods.FirstOrDefault(m => string.Equals(m.Id, request.PaymentMethodId, StringComparison.OrdinalIgnoreCase))
                            ?? throw new InvalidOperationException("The selected payment method is not available.");

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["shippingMethodId"] = shippingMethod.Id,
            ["paymentMethodId"] = paymentMethod.Id,
            ["paymentFlow"] = paymentMethod.Flow,
            ["returnUrl"] = request.ReturnUrl,
            ["cancelUrl"] = request.CancelUrl
        };

        if (request.PaymentData is not null)
        {
            foreach (var kvp in request.PaymentData)
            {
                metadata[$"payment:{kvp.Key}"] = kvp.Value;
            }
        }

        var shippingAddress = request.ShippingAddress;
        var formattedAddress = FormatAddress(shippingAddress);

        var checkoutRequest = new CheckoutRequest(
            request.CartId,
            request.UserId,
            request.GuestToken,
            shippingAddress.Email,
            formattedAddress,
            formattedAddress,
            shippingMethod.Currency,
            request.CouponCode,
            paymentMethod.Provider,
            metadata,
            shippingMethod.Amount,
            shippingMethod.Id,
            paymentMethod.Id);

        var response = await CheckoutAsync(checkoutRequest, cancellationToken);
        var order = response.Order;
        var paymentIntent = response.PaymentIntent;

        var status = paymentMethod.Flow switch
        {
            "hosted_redirect" => "requires_redirect",
            "inline_card" => "requires_client_confirmation",
            _ => "processing"
        };

        var redirectUrl = paymentMethod.Flow == "hosted_redirect" ? paymentIntent.PaymentUrl : null;
        var clientSecret = paymentMethod.Flow == "inline_card" ? paymentIntent.ClientSecret : null;

        return new CheckoutSessionDto(order.Id, status, redirectUrl, clientSecret);
    }

    public async Task<PaymentStatusDto?> GetPaymentStatusAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        var order = await _dbContext.Orders
            .Include(o => o.Payments)
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
        {
            return null;
        }

        var payment = order.Payments
            .OrderByDescending(p => p.ProcessedAt ?? order.CreatedAt)
            .FirstOrDefault();

        var paymentStatus = payment?.Status.ToString().ToLowerInvariant() ?? "pending";
        var orderStatus = order.Status.ToString().ToLowerInvariant();
        var updatedAt = payment?.ProcessedAt ?? order.UpdatedAt ?? order.CreatedAt;

        return new PaymentStatusDto(order.Id, orderStatus, paymentStatus, updatedAt);
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

        var categoryLookup = await _dbContext.ProductCategories
            .Where(pc => productIds.Contains(pc.ProductId))
            .GroupBy(pc => pc.ProductId)
            .ToDictionaryAsync(
                g => g.Key,
                g => (IReadOnlyCollection<Guid>)g.Select(pc => pc.CategoryId).ToList(),
                cancellationToken);

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
        var promotionContext = new PromotionEvaluationContext(
            tenantId,
            cart.Items.Select(item =>
            {
                var categories = categoryLookup.TryGetValue(item.ProductId, out var value)
                    ? value
                    : Array.Empty<Guid>();
                return new PromotionItemContext(
                    item.ProductId,
                    item.ProductVariantId,
                    item.Quantity,
                    item.UnitPrice,
                    categories);
            }).ToList(),
            subtotal,
            request.Currency,
            request.CouponCode);

        var promotionResult = await _promotionEngine.EvaluateAsync(promotionContext, cancellationToken);
        var discount = promotionResult.DiscountAmount;
        var taxableAmount = subtotal - discount;
        var tax = Math.Round(taxableAmount * DefaultTaxRate, 2, MidpointRounding.AwayFromZero);
        var shipping = request.ShippingAmount ?? DefaultShippingCost;
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
            CouponId = promotionResult.CouponId,
            CouponCode = promotionResult.CouponCode ?? request.CouponCode,
            PromotionCampaignId = promotionResult.PromotionCampaignId,
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

        if (promotionResult.CouponId.HasValue)
        {
            var appliedCoupon = await _dbContext.Coupons
                .FirstOrDefaultAsync(c => c.Id == promotionResult.CouponId.Value, cancellationToken);
            if (appliedCoupon is not null)
            {
                appliedCoupon.TimesRedeemed += 1;
            }
        }

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

    private async Task<string?> ResolveStoreCurrencyAsync(CancellationToken cancellationToken)
    {
        return await _dbContext.StoreSettings
            .AsNoTracking()
            .Where(s => s.TenantId == _tenantResolver.CurrentTenantId)
            .Select(s => s.Currency)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static string FormatAddress(CheckoutShippingAddressDto address)
    {
        var parts = new List<string>
        {
            address.FullName,
            address.AddressLine1
        };

        if (!string.IsNullOrWhiteSpace(address.AddressLine2))
        {
            parts.Add(address.AddressLine2);
        }

        var cityLine = string.IsNullOrWhiteSpace(address.Region)
            ? $"{address.City}, {address.PostalCode}"
            : $"{address.City}, {address.Region} {address.PostalCode}";
        parts.Add(cityLine);
        parts.Add(address.Country);

        if (!string.IsNullOrWhiteSpace(address.Email))
        {
            parts.Add(address.Email);
        }

        if (!string.IsNullOrWhiteSpace(address.Phone))
        {
            parts.Add(address.Phone);
        }

        return string.Join(Environment.NewLine, parts);
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
