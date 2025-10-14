namespace MultiTenantEcommerce.Application.Models;

public record RequestOtpRequest(string PhoneNumber, string Purpose = "login");
