using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Models;

public record CronJobRequest(
    string Name,
    string ScheduleExpression,
    string Handler,
    bool IsActive,
    DateTime? NextRunAt
);

public record CronJobResponse(
    Guid Id,
    string Name,
    string ScheduleExpression,
    string Handler,
    bool IsActive,
    DateTime? LastRunAt,
    DateTime? NextRunAt,
    DateTime CreatedAt
)
{
    public static CronJobResponse FromEntity(CronJob cronJob) => new(
        cronJob.Id,
        cronJob.Name,
        cronJob.ScheduleExpression,
        cronJob.Handler,
        cronJob.IsActive,
        cronJob.LastRunAt,
        cronJob.NextRunAt,
        cronJob.CreatedAt
    );
}
