using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models.Orders;

public record ShippingZoneDto(
    Guid Id,
    string Name,
    bool IsDefault,
    IReadOnlyList<ShippingZoneRegionDto> Regions,
    IReadOnlyList<ShippingMethodSummaryDto> Methods);

public record ShippingZoneRegionDto(string CountryCode, string? StateCode);

public record ShippingMethodSummaryDto(
    Guid Id,
    string Name,
    ShippingMethodType MethodType,
    string Currency,
    bool IsEnabled);

public record ShippingRateTableEntryDto(
    Guid Id,
    decimal MinValue,
    decimal? MaxValue,
    decimal Rate);

public record ShippingMethodDetailDto(
    Guid Id,
    Guid ShippingZoneId,
    string Name,
    string? Description,
    ShippingMethodType MethodType,
    ShippingRateConditionType RateConditionType,
    string Currency,
    decimal? FlatRate,
    decimal? MinimumOrderTotal,
    decimal? MaximumOrderTotal,
    bool IsEnabled,
    string? CarrierKey,
    string? CarrierServiceLevel,
    string? IntegrationSettingsJson,
    int? EstimatedTransitMinDays,
    int? EstimatedTransitMaxDays,
    IReadOnlyList<ShippingRateTableEntryDto> RateTable);

public record ShippingQuoteDto(
    Guid ShippingMethodId,
    string MethodName,
    decimal Amount,
    string Currency,
    int? EstimatedDaysMin,
    int? EstimatedDaysMax,
    string? Description);

public record TaxRuleDto(
    Guid Id,
    string CountryCode,
    string? StateCode,
    TaxCalculationType CalculationType,
    decimal Rate,
    bool AppliesToShipping,
    bool IsDefault);

public record TaxCalculationResult(decimal Amount, string Currency, Guid? AppliedRuleId);

public record ShippingCarrierQuoteRequest(
    string CarrierKey,
    string? ServiceLevel,
    CheckoutShippingAddressDto Destination,
    decimal OrderSubtotal,
    decimal TotalWeight,
    int TotalQuantity);
