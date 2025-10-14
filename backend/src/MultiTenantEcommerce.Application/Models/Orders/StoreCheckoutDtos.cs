namespace MultiTenantEcommerce.Application.Models.Orders;

public record CheckoutShippingMethodDto(
    string Id,
    string Name,
    decimal Amount,
    string Currency,
    string? Description,
    int? EstimatedDaysMin,
    int? EstimatedDaysMax);

public record CheckoutPaymentMethodDto(
    string Id,
    string Name,
    string Provider,
    string Flow,
    string? Instructions,
    IReadOnlyDictionary<string, string>? Metadata);

public record CheckoutConfigurationDto(
    IReadOnlyList<CheckoutShippingMethodDto> ShippingMethods,
    IReadOnlyList<CheckoutPaymentMethodDto> PaymentMethods);

public record CheckoutShippingAddressDto(
    string FullName,
    string AddressLine1,
    string? AddressLine2,
    string City,
    string? Region,
    string PostalCode,
    string Country,
    string Email,
    string? Phone);

public record CreateCheckoutSessionRequestDto(
    Guid? CartId,
    Guid? UserId,
    string? GuestToken,
    CheckoutShippingAddressDto ShippingAddress,
    string ShippingMethodId,
    string PaymentMethodId,
    string ReturnUrl,
    string CancelUrl,
    string? CouponCode,
    IReadOnlyDictionary<string, string>? PaymentData);

public record CheckoutSessionDto(
    Guid OrderId,
    string Status,
    string? RedirectUrl,
    string? ClientSecret);

public record PaymentStatusDto(
    Guid OrderId,
    string Status,
    string PaymentStatus,
    DateTime UpdatedAt);
