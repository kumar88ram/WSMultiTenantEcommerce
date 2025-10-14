using MultiTenantEcommerce.Application.Models.Promotions;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IPromotionEngine
{
    Task<PromotionEvaluationResult> EvaluateAsync(PromotionEvaluationContext context, CancellationToken cancellationToken = default);
}
