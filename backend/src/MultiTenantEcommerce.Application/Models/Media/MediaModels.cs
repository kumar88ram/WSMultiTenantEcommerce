namespace MultiTenantEcommerce.Application.Models.Media;

public record ImageResizeDefinition(string Name, int Width, int Height);

public record StoredFileReference(string AdapterName, string Path, string Url);

public record MediaFileDescriptor(string AdapterName, string Path, string Url, int Width, int Height);

public record MediaVariantDescriptor(string Name, string AdapterName, string Path, string Url, int Width, int Height);

public record MediaUploadResult(MediaFileDescriptor Original, IReadOnlyList<MediaVariantDescriptor> Variants);

public record SignedUrlResult(string AdapterName, string Url, DateTimeOffset ExpiresAt);
