using MultiTenantEcommerce.Application.Models;
using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface ITenantProvisioningService
{
    Task<Tenant> CreateTenantAsync(CreateTenantRequest request, CancellationToken cancellationToken = default);
}
