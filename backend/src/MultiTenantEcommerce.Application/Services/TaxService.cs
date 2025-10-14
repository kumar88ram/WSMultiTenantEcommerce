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

public class TaxService : ITaxService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<TaxService> _logger;

    public TaxService(ApplicationDbContext dbContext, ILogger<TaxService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<TaxCalculationResult> CalculateAsync(decimal taxableAmount, decimal shippingAmount, CheckoutShippingAddressDto address, CancellationToken cancellationToken = default)
    {
        var currency = await ResolveCurrencyAsync(cancellationToken);

        if (taxableAmount <= 0)
        {
            return new TaxCalculationResult(0m, currency, null);
        }

        var rules = await _dbContext.TaxRules
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (rules.Count == 0)
        {
            return new TaxCalculationResult(0m, currency, null);
        }

        var rule = ResolveRule(rules, address);
        if (rule is null)
        {
            return new TaxCalculationResult(0m, currency, null);
        }

        var baseAmount = taxableAmount + (rule.AppliesToShipping ? shippingAmount : 0m);
        decimal taxAmount = rule.CalculationType switch
        {
            TaxCalculationType.Percentage => Math.Round(baseAmount * rule.Rate, 2, MidpointRounding.AwayFromZero),
            TaxCalculationType.FixedAmount => rule.Rate,
            _ => 0m
        };

        return new TaxCalculationResult(taxAmount, currency, rule.Id);
    }

    public async Task<IReadOnlyList<TaxRuleDto>> GetRulesAsync(CancellationToken cancellationToken = default)
    {
        var rules = await _dbContext.TaxRules
            .AsNoTracking()
            .OrderByDescending(rule => rule.IsDefault)
            .ThenBy(rule => rule.CountryCode)
            .ThenBy(rule => rule.StateCode)
            .ToListAsync(cancellationToken);

        return rules.Select(MapRule).ToList();
    }

    public async Task<TaxRuleDto?> GetRuleByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var rule = await _dbContext.TaxRules
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        return rule is null ? null : MapRule(rule);
    }

    private static TaxRule? ResolveRule(IEnumerable<TaxRule> rules, CheckoutShippingAddressDto address)
    {
        TaxRule? fallback = null;
        foreach (var rule in rules)
        {
            if (!string.Equals(rule.CountryCode, address.Country, StringComparison.OrdinalIgnoreCase))
            {
                if (rule.IsDefault && fallback is null)
                {
                    fallback = rule;
                }

                continue;
            }

            if (string.IsNullOrWhiteSpace(rule.StateCode))
            {
                fallback = rule;
                continue;
            }

            if (!string.IsNullOrWhiteSpace(address.Region) && string.Equals(rule.StateCode, address.Region, StringComparison.OrdinalIgnoreCase))
            {
                return rule;
            }
        }

        return fallback;
    }

    private async Task<string> ResolveCurrencyAsync(CancellationToken cancellationToken)
    {
        var currency = await _dbContext.StoreSettings
            .AsNoTracking()
            .Select(s => s.Currency)
            .FirstOrDefaultAsync(cancellationToken);

        if (string.IsNullOrWhiteSpace(currency))
        {
            _logger.LogDebug("Store currency not configured; defaulting to USD for tax calculations");
            return "USD";
        }

        return currency!;
    }

    private static TaxRuleDto MapRule(TaxRule rule) => new(
        rule.Id,
        rule.CountryCode,
        rule.StateCode,
        rule.CalculationType,
        rule.Rate,
        rule.AppliesToShipping,
        rule.IsDefault);
}
