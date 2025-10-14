using MultiTenantEcommerce.Application.Models.Media;

namespace MultiTenantEcommerce.Application.Abstractions;

public interface IMediaService
{
    Task<MediaUploadResult> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        IReadOnlyCollection<ImageResizeDefinition>? resizeDefinitions,
        CancellationToken cancellationToken);

    Task<SignedUrlResult> GetSignedUrlAsync(
        string path,
        TimeSpan expiresIn,
        string? adapterName,
        CancellationToken cancellationToken);
}
