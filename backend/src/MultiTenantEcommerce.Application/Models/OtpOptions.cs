namespace MultiTenantEcommerce.Application.Models;

public class OtpOptions
{
    public int CodeLength { get; set; } = 6;
    public int ExpiryMinutes { get; set; } = 5;
    public int MaxVerificationAttempts { get; set; } = 5;
    public int ResendCooldownSeconds { get; set; } = 30;
}
