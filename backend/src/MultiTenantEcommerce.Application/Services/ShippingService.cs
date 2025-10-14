using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Orders;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class ShippingService : IShippingService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly IEnumerable<IShippingCarrierAdapter> _carrierAdapters;
    private readonly ILogger<ShippingService> _logger;

    public ShippingService(
        ApplicationDbContext dbContext,
        IEnumerable<IShippingCarrierAdapter> carrierAdapters,
        ILogger<ShippingService> logger)
    {
        _dbContext = dbContext;
        _carrierAdapters = carrierAdapters;
        _logger = logger;
    }

    public async Task<IReadOnlyList<ShippingZoneDto>> GetZonesAsync(CancellationToken cancellationToken = default)
    {
        var zones = await _dbContext.ShippingZones
            .AsNoTracking()
            .Include(z => z.Regions)
            .Include(z => z.Methods)
            .OrderByDescending(z => z.IsDefault)
            .ThenBy(z => z.Name)
            .ToListAsync(cancellationToken);

        return zones.Select(MapZone).ToList();
    }

    public async Task<ShippingZoneDto?> GetZoneByIdAsync(Guid zoneId, CancellationToken cancellationToken = default)
    {
        var zone = await _dbContext.ShippingZones
            .AsNoTracking()
            .Include(z => z.Regions)
            .Include(z => z.Methods)
            .FirstOrDefaultAsync(z => z.Id == zoneId, cancellationToken);

        return zone is null ? null : MapZone(zone);
    }

    public async Task<ShippingMethodDetailDto?> GetMethodByIdAsync(Guid methodId, CancellationToken cancellationToken = default)
    {
        var method = await _dbContext.ShippingMethods
            .AsNoTracking()
            .Include(m => m.RateTable)
            .FirstOrDefaultAsync(m => m.Id == methodId, cancellationToken);

        return method is null ? null : MapMethodDetail(method);
    }

    public async Task<IReadOnlyList<CheckoutShippingMethodDto>> GetCheckoutMethodsAsync(CheckoutShippingAddressDto? address, CancellationToken cancellationToken = default)
    {
        var methodsQuery = _dbContext.ShippingMethods
            .AsNoTracking()
            .Include(m => m.ShippingZone)
            .ThenInclude(z => z!.Regions)
            .Where(m => m.IsEnabled);

        var methods = await methodsQuery
            .OrderBy(m => m.Name)
            .ToListAsync(cancellationToken);

        if (address is not null)
        {
            methods = FilterByAddress(methods, address).ToList();
        }

        if (!methods.Any())
        {
            // fallback to all enabled methods if no zone matches the address
            methods = await methodsQuery.ToListAsync(cancellationToken);
        }

        return methods
            .Select(method => new CheckoutShippingMethodDto(
                method.Id.ToString(),
                method.Name,
                method.MethodType,
                method.FlatRate ?? 0m,
                method.Currency,
                method.Description,
                method.EstimatedTransitMinDays,
                method.EstimatedTransitMaxDays))
            .ToList();
    }

    public async Task<ShippingQuoteDto> QuoteAsync(Guid shippingMethodId, CheckoutShippingAddressDto address, IReadOnlyCollection<CartItem> cartItems, CancellationToken cancellationToken = default)
    {
        var method = await _dbContext.ShippingMethods
            .Include(m => m.RateTable)
            .Include(m => m.ShippingZone)
            .ThenInclude(z => z!.Regions)
            .FirstOrDefaultAsync(m => m.Id == shippingMethodId, cancellationToken)
            ?? throw new InvalidOperationException("Shipping method not found");

        if (!method.IsEnabled)
        {
            throw new InvalidOperationException("Shipping method is not available");
        }

        if (method.ShippingZone is not null && !ZoneMatches(method.ShippingZone, address))
        {
            throw new InvalidOperationException("Shipping method is not available for the selected address");
        }

        var subtotal = cartItems.Sum(item => item.UnitPrice * item.Quantity);
        var totalQuantity = cartItems.Sum(item => item.Quantity);
        var totalWeight = cartItems.Sum(item => item.Quantity); // weight placeholder; extend when weight metadata is available

        if (method.MinimumOrderTotal.HasValue && subtotal < method.MinimumOrderTotal.Value)
        {
            throw new InvalidOperationException("Order total does not meet the minimum required for this shipping method");
        }

        if (method.MaximumOrderTotal.HasValue && subtotal > method.MaximumOrderTotal.Value)
        {
            throw new InvalidOperationException("Order total exceeds the maximum allowed for this shipping method");
        }

        var amount = await CalculateShippingAmountAsync(method, address, subtotal, totalWeight, totalQuantity, cancellationToken);

        return new ShippingQuoteDto(
            method.Id,
            method.Name,
            amount,
            method.Currency,
            method.EstimatedTransitMinDays,
            method.EstimatedTransitMaxDays,
            method.Description);
    }

    private static ShippingZoneDto MapZone(ShippingZone zone)
    {
        var regions = zone.Regions
            .Select(region => new ShippingZoneRegionDto(region.CountryCode, region.StateCode))
            .ToList();

        var methods = zone.Methods
            .Select(method => new ShippingMethodSummaryDto(method.Id, method.Name, method.MethodType, method.Currency, method.IsEnabled))
            .ToList();

        return new ShippingZoneDto(zone.Id, zone.Name, zone.IsDefault, regions, methods);
    }

    private static ShippingMethodDetailDto MapMethodDetail(ShippingMethod method)
    {
        var rateTable = method.RateTable
            .OrderBy(entry => entry.MinValue)
            .Select(entry => new ShippingRateTableEntryDto(entry.Id, entry.MinValue, entry.MaxValue, entry.Rate))
            .ToList();

        return new ShippingMethodDetailDto(
            method.Id,
            method.ShippingZoneId,
            method.Name,
            method.Description,
            method.MethodType,
            method.RateConditionType,
            method.Currency,
            method.FlatRate,
            method.MinimumOrderTotal,
            method.MaximumOrderTotal,
            method.IsEnabled,
            method.CarrierKey,
            method.CarrierServiceLevel,
            method.IntegrationSettingsJson,
            method.EstimatedTransitMinDays,
            method.EstimatedTransitMaxDays,
            rateTable);
    }

    private IEnumerable<ShippingMethod> FilterByAddress(IEnumerable<ShippingMethod> methods, CheckoutShippingAddressDto address)
    {
        foreach (var method in methods)
        {
            if (method.ShippingZone is null || !method.ShippingZone.Regions.Any())
            {
                if (method.ShippingZone?.IsDefault ?? true)
                {
                    yield return method;
                }

                continue;
            }

            if (ZoneMatches(method.ShippingZone, address))
            {
                yield return method;
            }
        }
    }

    private static bool ZoneMatches(ShippingZone zone, CheckoutShippingAddressDto address)
    {
        if (zone.Regions.Count == 0)
        {
            return zone.IsDefault;
        }

        foreach (var region in zone.Regions)
        {
            if (!string.Equals(region.CountryCode, address.Country, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(region.StateCode))
            {
                return true;
            }

            if (!string.IsNullOrWhiteSpace(address.Region) && string.Equals(region.StateCode, address.Region, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<decimal> CalculateShippingAmountAsync(
        ShippingMethod method,
        CheckoutShippingAddressDto address,
        decimal orderSubtotal,
        decimal totalWeight,
        int totalQuantity,
        CancellationToken cancellationToken)
    {
        return method.MethodType switch
        {
            ShippingMethodType.FlatRate => method.FlatRate ?? 0m,
            ShippingMethodType.WeightBased or ShippingMethodType.RateTable =>
                ResolveRateFromTable(method, orderSubtotal, totalWeight, totalQuantity),
            ShippingMethodType.External => await QuoteFromCarrierAsync(method, address, orderSubtotal, totalWeight, totalQuantity, cancellationToken),
            _ => method.FlatRate ?? 0m
        };
    }

    private decimal ResolveRateFromTable(ShippingMethod method, decimal orderSubtotal, decimal totalWeight, int totalQuantity)
    {
        var value = method.RateConditionType switch
        {
            ShippingRateConditionType.Weight => totalWeight,
            ShippingRateConditionType.OrderTotal => orderSubtotal,
            _ => orderSubtotal
        };

        var ordered = method.RateTable.OrderBy(entry => entry.MinValue).ToList();
        foreach (var entry in ordered)
        {
            if (value >= entry.MinValue && (!entry.MaxValue.HasValue || value <= entry.MaxValue.Value))
            {
                return entry.Rate;
            }
        }

        // fallback to the highest tier if no tier matched the exact range
        var last = ordered.LastOrDefault();
        return last?.Rate ?? method.FlatRate ?? 0m;
    }

    private async Task<decimal> QuoteFromCarrierAsync(
        ShippingMethod method,
        CheckoutShippingAddressDto address,
        decimal orderSubtotal,
        decimal totalWeight,
        int totalQuantity,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(method.CarrierKey))
        {
            return method.FlatRate ?? 0m;
        }

        var adapter = _carrierAdapters.FirstOrDefault(a => string.Equals(a.CarrierKey, method.CarrierKey, StringComparison.OrdinalIgnoreCase));
        if (adapter is null)
        {
            _logger.LogWarning("No shipping carrier adapter registered for key {CarrierKey}", method.CarrierKey);
            return method.FlatRate ?? 0m;
        }

        var request = new ShippingCarrierQuoteRequest(
            method.CarrierKey,
            method.CarrierServiceLevel,
            address,
            orderSubtotal,
            totalWeight,
            totalQuantity);

        var quote = await adapter.QuoteAsync(request, cancellationToken);
        if (quote is null)
        {
            _logger.LogWarning("Carrier {CarrierKey} returned no quote for method {MethodId}", method.CarrierKey, method.Id);
            return method.FlatRate ?? 0m;
        }

        return quote.Amount;
    }
}
