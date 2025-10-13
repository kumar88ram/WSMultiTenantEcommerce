using MultiTenantEcommerce.Domain.Entities;

namespace MultiTenantEcommerce.Infrastructure.Security;

public interface ITokenFactory
{
    TokenPair CreateTokenPair(User user);
}
