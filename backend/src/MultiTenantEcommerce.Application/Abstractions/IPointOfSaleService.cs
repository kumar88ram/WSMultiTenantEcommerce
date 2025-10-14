using MultiTenantEcommerce.Application.Models.Pos;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPointOfSaleService
{
    Task<PosSaleResponse> CreateOfflineSaleAsync(PosSaleRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<InventorySyncResult>> SyncInventoryAsync(IEnumerable<InventorySyncRequest> requests, CancellationToken cancellationToken = default);

    Task<ReceiptResponse> GenerateReceiptAsync(Guid orderId, CancellationToken cancellationToken = default);
}
