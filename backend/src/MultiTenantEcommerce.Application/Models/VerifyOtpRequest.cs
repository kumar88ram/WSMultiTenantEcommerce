namespace MultiTenantEcommerce.Application.Models;

public record VerifyOtpRequest(string PhoneNumber, string Code, string Purpose = "login");
