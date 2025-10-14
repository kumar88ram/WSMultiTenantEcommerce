using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Application.Models.Payments;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class RefundService : IRefundService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IPaymentGatewayOrchestrator _paymentGatewayOrchestrator;
    private readonly IEmailNotificationQueue _emailQueue;
    private readonly ILogger<RefundService> _logger;

    public RefundService(
        ApplicationDbContext dbContext,
        IPaymentGatewayOrchestrator paymentGatewayOrchestrator,
        IEmailNotificationQueue emailQueue,
        ILogger<RefundService> logger)
    {
        _dbContext = dbContext;
        _paymentGatewayOrchestrator = paymentGatewayOrchestrator;
        _emailQueue = emailQueue;
        _logger = logger;
    }

    public async Task<RefundRequestDto> SubmitAsync(CreateRefundRequestDto request, CancellationToken cancellationToken = default)
    {
        if (request.Items.Count == 0)
        {
            throw new ArgumentException("At least one item must be provided", nameof(request.Items));
        }

        var order = await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == request.OrderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found");

        var (refundRequest, totalAmount) = BuildRefundRequest(order, request);
        refundRequest.TenantId = order.TenantId;
        refundRequest.RequestedAmount = totalAmount;
        refundRequest.ApprovedAmount = null;
        refundRequest.Status = RefundRequestStatus.Pending;

        await _dbContext.RefundRequests.AddAsync(refundRequest, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailQueue.QueueAsync(new OrderEmailNotification(
            OrderEmailNotificationType.RefundRequested,
            order.TenantId,
            order.Email,
            order.OrderNumber,
            order.GrandTotal,
            order.Currency,
            order.CreatedAt,
            null,
            totalAmount,
            request.Reason),
            cancellationToken);

        refundRequest.Order = order;
        return MapRefundRequest(refundRequest, order.Currency);
    }

    public async Task<RefundRequestDto> ApproveAsync(Guid refundRequestId, RefundDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var refund = await LoadRefundRequestAsync(refundRequestId, cancellationToken);
        if (refund is null)
        {
            throw new InvalidOperationException("Refund request not found");
        }

        if (refund.Status != RefundRequestStatus.Pending)
        {
            throw new InvalidOperationException("Refund request has already been processed");
        }

        refund.Status = RefundRequestStatus.Approved;
        refund.DecisionAt = DateTime.UtcNow;
        refund.DecisionNotes = request.Notes;
        refund.ApprovedAmount = refund.RequestedAmount;

        await _emailQueue.QueueAsync(new OrderEmailNotification(
            OrderEmailNotificationType.RefundApproved,
            refund.Order!.TenantId,
            refund.Order.Email,
            refund.Order.OrderNumber,
            refund.Order.GrandTotal,
            refund.Order.Currency,
            refund.Order.CreatedAt,
            null,
            refund.ApprovedAmount,
            request.Notes),
            cancellationToken);

        await ProcessRefundAsync(refund, refund.ApprovedAmount ?? refund.RequestedAmount, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapRefundRequest(refund, refund.Order!.Currency);
    }

    public async Task<RefundRequestDto> DenyAsync(Guid refundRequestId, RefundDecisionRequest request, CancellationToken cancellationToken = default)
    {
        var refund = await LoadRefundRequestAsync(refundRequestId, cancellationToken)
            ?? throw new InvalidOperationException("Refund request not found");

        if (refund.Status != RefundRequestStatus.Pending)
        {
            throw new InvalidOperationException("Refund request has already been processed");
        }

        refund.Status = RefundRequestStatus.Denied;
        refund.DecisionAt = DateTime.UtcNow;
        refund.DecisionNotes = request.Notes;

        await _dbContext.SaveChangesAsync(cancellationToken);

        await _emailQueue.QueueAsync(new OrderEmailNotification(
            OrderEmailNotificationType.RefundDenied,
            refund.Order!.TenantId,
            refund.Order.Email,
            refund.Order.OrderNumber,
            refund.Order.GrandTotal,
            refund.Order.Currency,
            refund.Order.CreatedAt,
            null,
            refund.RequestedAmount,
            request.Notes),
            cancellationToken);

        return MapRefundRequest(refund, refund.Order!.Currency);
    }

    public async Task<RefundRequestDto?> GetByIdAsync(Guid refundRequestId, CancellationToken cancellationToken = default)
    {
        var refund = await _dbContext.RefundRequests
            .AsNoTracking()
            .Include(r => r.Order)
            .ThenInclude(o => o!.Items)
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .FirstOrDefaultAsync(r => r.Id == refundRequestId, cancellationToken);

        return refund is null || refund.Order is null
            ? null
            : MapRefundRequest(refund, refund.Order.Currency);
    }

    public async Task<RefundRequestListResult> GetAsync(RefundRequestListQuery query, CancellationToken cancellationToken = default)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 200);

        var refundQuery = _dbContext.RefundRequests
            .AsNoTracking()
            .Include(r => r.Order)
            .Where(r => r.Order != null);

        if (query.Status.HasValue)
        {
            refundQuery = refundQuery.Where(r => r.Status == query.Status.Value);
        }

        var totalCount = await refundQuery.CountAsync(cancellationToken);
        var items = await refundQuery
            .OrderByDescending(r => r.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var summaries = items
            .Where(r => r.Order is not null)
            .Select(r => new RefundRequestSummaryDto(
                r.Id,
                r.OrderId,
                r.Order!.OrderNumber,
                r.Status,
                r.RequestedAmount,
                r.ApprovedAmount,
                r.CreatedAt,
                r.DecisionAt))
            .ToList();

        return new RefundRequestListResult(page, pageSize, totalCount, summaries);
    }

    public async Task<OrderDto> CreateImmediateRefundAsync(Guid orderId, OrderRefundCommand request, CancellationToken cancellationToken = default)
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

        var refund = new RefundRequest
        {
            TenantId = order.TenantId,
            Order = order,
            OrderId = order.Id,
            Reason = string.IsNullOrWhiteSpace(request.Reason) ? "Manual refund" : request.Reason,
            RequestedAmount = request.Amount,
            ApprovedAmount = request.Amount,
            Status = RefundRequestStatus.Approved,
            DecisionAt = DateTime.UtcNow
        };

        await _dbContext.RefundRequests.AddAsync(refund, cancellationToken);
        await ProcessRefundAsync(refund, request.Amount, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return MapOrder(order);
    }

    private (RefundRequest refundRequest, decimal totalAmount) BuildRefundRequest(Order order, CreateRefundRequestDto request)
    {
        var refund = new RefundRequest
        {
            Order = order,
            OrderId = order.Id,
            Reason = request.Reason,
            Status = RefundRequestStatus.Pending
        };

        decimal total = 0;
        foreach (var item in request.Items)
        {
            var orderItem = order.Items.FirstOrDefault(oi => oi.Id == item.OrderItemId)
                ?? throw new InvalidOperationException("Order item not found");

            if (item.Quantity <= 0 || item.Quantity > orderItem.Quantity)
            {
                throw new InvalidOperationException("Requested quantity exceeds purchased quantity");
            }

            var lineTotal = orderItem.UnitPrice * item.Quantity;
            total += lineTotal;

            refund.Items.Add(new RefundRequestItem
            {
                TenantId = order.TenantId,
                OrderItem = orderItem,
                OrderItemId = orderItem.Id,
                Quantity = item.Quantity,
                LineTotal = lineTotal
            });
        }

        return (refund, total);
    }

    private async Task<RefundRequest?> LoadRefundRequestAsync(Guid refundRequestId, CancellationToken cancellationToken)
    {
        return await _dbContext.RefundRequests
            .Include(r => r.Items)
            .ThenInclude(i => i.OrderItem)
            .Include(r => r.Order)
            .ThenInclude(o => o!.Items)
            .Include(r => r.Order)
            .ThenInclude(o => o!.Payments)
            .FirstOrDefaultAsync(r => r.Id == refundRequestId, cancellationToken);
    }

    private async Task ProcessRefundAsync(RefundRequest refund, decimal amount, CancellationToken cancellationToken)
    {
        var order = refund.Order ?? throw new InvalidOperationException("Refund request has no associated order");

        var capturedPayment = order.Payments
            .OrderByDescending(p => p.ProcessedAt ?? order.CreatedAt)
            .FirstOrDefault(p => p.Status is PaymentStatus.Captured or PaymentStatus.Authorized);

        PaymentStatus refundStatus = PaymentStatus.Refunded;
        if (capturedPayment is not null && !string.IsNullOrWhiteSpace(capturedPayment.Provider))
        {
            var refundRequestModel = new PaymentRefundRequest(
                capturedPayment.ProviderReference,
                amount,
                order.Currency,
                refund.Reason);

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

                refundStatus = PaymentStatus.Failed;
            }
        }
        else
        {
            _logger.LogWarning("No captured payment found for order {OrderNumber}", order.OrderNumber);
        }

        var payment = new PaymentTransaction
        {
            TenantId = order.TenantId,
            Order = order,
            Provider = capturedPayment?.Provider ?? "manual",
            ProviderReference = $"refund_{Guid.NewGuid():N}",
            Amount = -Math.Abs(amount),
            Currency = order.Currency,
            Status = refundStatus,
            RawPayload = refund.Reason,
            ProcessedAt = refundStatus == PaymentStatus.Refunded ? DateTime.UtcNow : null
        };

        order.Payments.Add(payment);
        refund.PaymentTransaction = payment;
        refund.PaymentTransactionId = payment.Id;

        if (refundStatus == PaymentStatus.Refunded)
        {
            refund.Status = RefundRequestStatus.Refunded;
            refund.ProcessedAt = payment.ProcessedAt ?? DateTime.UtcNow;
            order.Status = OrderStatus.Refunded;
            order.UpdatedAt = DateTime.UtcNow;
            await ReleaseInventoryAsync(order, cancellationToken);

            await _emailQueue.QueueAsync(new OrderEmailNotification(
                OrderEmailNotificationType.RefundProcessed,
                order.TenantId,
                order.Email,
                order.OrderNumber,
                order.GrandTotal,
                order.Currency,
                order.CreatedAt,
                null,
                amount,
                refund.DecisionNotes ?? refund.Reason),
                cancellationToken);
        }
        else
        {
            refund.Status = RefundRequestStatus.Failed;

            await _emailQueue.QueueAsync(new OrderEmailNotification(
                OrderEmailNotificationType.RefundFailed,
                order.TenantId,
                order.Email,
                order.OrderNumber,
                order.GrandTotal,
                order.Currency,
                order.CreatedAt,
                null,
                amount,
                "The refund could not be processed. Please contact support."),
                cancellationToken);
        }
    }

    private async Task ReleaseInventoryAsync(Order order, CancellationToken cancellationToken)
    {
        var productIds = order.Items.Select(item => item.ProductId).Distinct().ToList();
        var inventories = await _dbContext.Inventories
            .Where(i => productIds.Contains(i.ProductId))
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

    private static RefundRequestDto MapRefundRequest(RefundRequest refund, string currency)
    {
        var items = refund.Items
            .Select(item => new RefundRequestItemDto(
                item.OrderItemId,
                item.OrderItem?.Name ?? string.Empty,
                item.Quantity,
                item.LineTotal))
            .ToList();

        return new RefundRequestDto(
            refund.Id,
            refund.OrderId,
            refund.Order?.OrderNumber ?? string.Empty,
            refund.Status,
            refund.Reason,
            refund.RequestedAmount,
            refund.ApprovedAmount,
            refund.CreatedAt,
            refund.DecisionAt,
            refund.ProcessedAt,
            items,
            refund.PaymentTransactionId,
            currency);
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
                payment.ProcessedAt ?? order.CreatedAt))
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
}
