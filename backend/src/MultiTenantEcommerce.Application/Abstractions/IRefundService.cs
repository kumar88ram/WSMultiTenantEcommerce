using MultiTenantEcommerce.Application.Models.Orders;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IRefundService
{
    Task<RefundRequestDto> SubmitAsync(CreateRefundRequestDto request, CancellationToken cancellationToken = default);
    Task<RefundRequestDto> ApproveAsync(Guid refundRequestId, RefundDecisionRequest request, CancellationToken cancellationToken = default);
    Task<RefundRequestDto> DenyAsync(Guid refundRequestId, RefundDecisionRequest request, CancellationToken cancellationToken = default);
    Task<RefundRequestDto?> GetByIdAsync(Guid refundRequestId, CancellationToken cancellationToken = default);
    Task<RefundRequestListResult> GetAsync(RefundRequestListQuery query, CancellationToken cancellationToken = default);
    Task<OrderDto> CreateImmediateRefundAsync(Guid orderId, OrderRefundCommand request, CancellationToken cancellationToken = default);
}
