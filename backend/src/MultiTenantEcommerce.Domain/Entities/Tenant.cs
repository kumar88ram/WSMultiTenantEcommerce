namespace MultiTenantEcommerce.Domain.Entities;

public class Tenant : BaseEntity
{
    public string Identifier { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public ICollection<User> Users { get; set; } = new List<User>();

    public Tenant()
    {
        TenantId = Id;
    }
}
