namespace MultiTenantEcommerce.Application.Abstractions;

public interface IFileStorageAdapterResolver
{
    IFileStorageAdapter GetDefaultAdapter();

    IFileStorageAdapter GetAdapter(string? name);
}
