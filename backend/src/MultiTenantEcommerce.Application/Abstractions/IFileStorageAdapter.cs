using MultiTenantEcommerce.Application.Models.Media;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IFileStorageAdapter
{
    string Name { get; }

    Task<StoredFileReference> SaveAsync(Stream content, string path, string contentType, CancellationToken cancellationToken);

    Task DeleteAsync(string path, CancellationToken cancellationToken);

    Task<string> GetPublicUrlAsync(string path, CancellationToken cancellationToken);

    Task<string> GetSignedUrlAsync(string path, TimeSpan expiresIn, CancellationToken cancellationToken);
}
