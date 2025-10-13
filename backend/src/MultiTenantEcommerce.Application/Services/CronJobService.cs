using System.Linq;
using Microsoft.EntityFrameworkCore;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;
using MultiTenantEcommerce.Infrastructure.Persistence;

namespace MultiTenantEcommerce.Application.Services;

public class CronJobService : ICronJobService
{
    private readonly AdminDbContext _dbContext;

    public CronJobService(AdminDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IEnumerable<CronJobResponse>> GetAsync(CancellationToken cancellationToken = default)
    {
        var cronJobs = await _dbContext.CronJobs
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return cronJobs.Select(CronJobResponse.FromEntity);
    }

    public async Task<CronJobResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cronJob = await _dbContext.CronJobs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return cronJob is null ? null : CronJobResponse.FromEntity(cronJob);
    }

    public async Task<CronJobResponse> CreateAsync(CronJobRequest request, CancellationToken cancellationToken = default)
    {
        var cronJob = new CronJob
        {
            Name = request.Name.Trim(),
            ScheduleExpression = request.ScheduleExpression.Trim(),
            Handler = request.Handler.Trim(),
            IsActive = request.IsActive,
            NextRunAt = request.NextRunAt
        };

        _dbContext.CronJobs.Add(cronJob);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CronJobResponse.FromEntity(cronJob);
    }

    public async Task<CronJobResponse?> UpdateAsync(Guid id, CronJobRequest request, CancellationToken cancellationToken = default)
    {
        var cronJob = await _dbContext.CronJobs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cronJob is null)
        {
            return null;
        }

        cronJob.Name = request.Name.Trim();
        cronJob.ScheduleExpression = request.ScheduleExpression.Trim();
        cronJob.Handler = request.Handler.Trim();
        cronJob.IsActive = request.IsActive;
        cronJob.NextRunAt = request.NextRunAt;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return CronJobResponse.FromEntity(cronJob);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var cronJob = await _dbContext.CronJobs.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        if (cronJob is null)
        {
            return false;
        }

        _dbContext.CronJobs.Remove(cronJob);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
