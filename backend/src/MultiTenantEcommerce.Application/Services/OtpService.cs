using System;
using System.Linq;
using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class OtpService : IOtpService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ISmsSender _smsSender;
    private readonly ILogger<OtpService> _logger;
    private readonly OtpOptions _options;

    public OtpService(
        ApplicationDbContext dbContext,
        ISmsSender smsSender,
        IOptions<OtpOptions> options,
        ILogger<OtpService> logger)
    {
        _dbContext = dbContext;
        _smsSender = smsSender;
        _logger = logger;
        _options = options.Value;
    }

    public async Task RequestOtpAsync(User user, string purpose, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            throw new InvalidOperationException("User does not have a phone number configured for OTP delivery.");
        }

        var normalizedPurpose = NormalizePurpose(purpose);
        var now = DateTime.UtcNow;

        var mostRecent = await _dbContext.OneTimePasswords
            .Where(o => o.UserId == user.Id && o.Purpose == normalizedPurpose)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (mostRecent is not null && now - mostRecent.CreatedAt < TimeSpan.FromSeconds(_options.ResendCooldownSeconds))
        {
            _logger.LogInformation("OTP request throttled for user {UserId} and purpose {Purpose}", user.Id, normalizedPurpose);
            return;
        }

        var code = GenerateCode(_options.CodeLength);
        var otp = new OneTimePassword
        {
            UserId = user.Id,
            TenantId = user.TenantId,
            Code = code,
            Purpose = normalizedPurpose,
            Destination = user.PhoneNumber!,
            ExpiresAt = now.AddMinutes(_options.ExpiryMinutes)
        };

        _dbContext.OneTimePasswords.Add(otp);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var message = $"Your verification code is {code}. It expires in {_options.ExpiryMinutes} minutes.";
        await _smsSender.SendAsync(user.PhoneNumber!, message, cancellationToken);
    }

    public async Task<bool> ValidateOtpAsync(User user, string code, string purpose, CancellationToken cancellationToken = default)
    {
        var normalizedPurpose = NormalizePurpose(purpose);
        var now = DateTime.UtcNow;

        var otp = await _dbContext.OneTimePasswords
            .Where(o => o.UserId == user.Id && o.Purpose == normalizedPurpose)
            .OrderByDescending(o => o.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (otp is null)
        {
            _logger.LogWarning("No OTP found for user {UserId} and purpose {Purpose}", user.Id, normalizedPurpose);
            return false;
        }

        if (otp.IsVerified || otp.IsExpired)
        {
            _logger.LogInformation("OTP expired or already used for user {UserId} and purpose {Purpose}", user.Id, normalizedPurpose);
            return false;
        }

        if (otp.AttemptCount >= _options.MaxVerificationAttempts)
        {
            _logger.LogWarning("OTP attempts exceeded for user {UserId} and purpose {Purpose}", user.Id, normalizedPurpose);
            return false;
        }

        otp.AttemptCount += 1;

        var isMatch = string.Equals(otp.Code, code, StringComparison.Ordinal);
        if (!isMatch)
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
            return false;
        }

        otp.VerifiedAt = now;
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static string NormalizePurpose(string purpose) =>
        string.IsNullOrWhiteSpace(purpose) ? "login" : purpose.Trim().ToLowerInvariant();

    private string GenerateCode(int length)
    {
        var max = (int)Math.Pow(10, length);
        Span<byte> bytes = stackalloc byte[4];
        RandomNumberGenerator.Fill(bytes);
        var value = BitConverter.ToUInt32(bytes);
        var code = (int)(value % max);
        return code.ToString($"D{length}");
    }
}
