using System.Globalization;
using Microsoft.Extensions.Logging;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Media;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;

namespace MultiTenantEcommerce.Application.Services;

public class MediaService : IMediaService
{
    private static readonly IReadOnlyList<ImageResizeDefinition> DefaultResizes =
        new List<ImageResizeDefinition>
        {
            new("thumbnail", 320, 320),
            new("medium", 768, 768)
        };

    private readonly IFileStorageAdapterResolver _adapterResolver;
    private readonly ITenantResolver _tenantResolver;
    private readonly ILogger<MediaService> _logger;

    public MediaService(
        IFileStorageAdapterResolver adapterResolver,
        ITenantResolver tenantResolver,
        ILogger<MediaService> logger)
    {
        _adapterResolver = adapterResolver;
        _tenantResolver = tenantResolver;
        _logger = logger;
    }

    public async Task<MediaUploadResult> UploadImageAsync(
        Stream fileStream,
        string fileName,
        string? contentType,
        IReadOnlyCollection<ImageResizeDefinition>? resizeDefinitions,
        CancellationToken cancellationToken)
    {
        if (fileStream is null)
        {
            throw new ArgumentNullException(nameof(fileStream));
        }

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("A file name must be provided.", nameof(fileName));
        }

        var adapter = _adapterResolver.GetDefaultAdapter();

        await using var bufferedStream = new MemoryStream();
        await fileStream.CopyToAsync(bufferedStream, cancellationToken);
        bufferedStream.Position = 0;

        var imageFormat = Image.DetectFormat(bufferedStream)
                          ?? throw new InvalidOperationException("Unsupported image format.");

        var encoder = Configuration.Default.ImageFormatsManager.FindEncoder(imageFormat)
                      ?? throw new InvalidOperationException("No encoder available for the provided image format.");

        bufferedStream.Position = 0;
        using var image = await Image.LoadAsync(bufferedStream, cancellationToken);

        var tenantSegment = !string.IsNullOrWhiteSpace(_tenantResolver.TenantIdentifier)
            ? _tenantResolver.TenantIdentifier!
            : _tenantResolver.CurrentTenantId.ToString("N", CultureInfo.InvariantCulture);

        var extension = GetFileExtension(fileName, imageFormat);
        var normalizedContentType = !string.IsNullOrWhiteSpace(contentType)
            ? contentType
            : imageFormat.DefaultMimeType ?? "application/octet-stream";

        var baseKey = $"{tenantSegment}/{DateTime.UtcNow:yyyy/MM}/{Guid.NewGuid():N}";
        var originalPath = $"{baseKey}{extension}";

        var originalBytes = bufferedStream.ToArray();
        await using var originalStream = new MemoryStream(originalBytes);
        var storedOriginal = await adapter.SaveAsync(originalStream, originalPath, normalizedContentType, cancellationToken);

        var originalDescriptor = new MediaFileDescriptor(
            storedOriginal.AdapterName,
            storedOriginal.Path,
            storedOriginal.Url,
            image.Width,
            image.Height);

        var definitions = (resizeDefinitions is null || resizeDefinitions.Count == 0)
            ? DefaultResizes
            : resizeDefinitions;

        var variants = new List<MediaVariantDescriptor>(definitions.Count);
        foreach (var definition in definitions)
        {
            if (definition.Width <= 0 || definition.Height <= 0)
            {
                _logger.LogWarning("Skipping resize definition {Definition} because dimensions are invalid.", definition);
                continue;
            }

            using var resizedImage = image.Clone(ctx =>
                ctx.Resize(new ResizeOptions
                {
                    Size = new Size(definition.Width, definition.Height),
                    Mode = ResizeMode.Max
                }));

            await using var variantStream = new MemoryStream();
            await resizedImage.SaveAsync(variantStream, encoder, cancellationToken);
            variantStream.Position = 0;

            var variantPath = $"{baseKey}_{definition.Name}{extension}";
            var storedVariant = await adapter.SaveAsync(variantStream, variantPath, normalizedContentType, cancellationToken);

            variants.Add(new MediaVariantDescriptor(
                definition.Name,
                storedVariant.AdapterName,
                storedVariant.Path,
                storedVariant.Url,
                resizedImage.Width,
                resizedImage.Height));
        }

        return new MediaUploadResult(originalDescriptor, variants);
    }

    public Task<SignedUrlResult> GetSignedUrlAsync(
        string path,
        TimeSpan expiresIn,
        string? adapterName,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path must be provided.", nameof(path));
        }

        if (expiresIn <= TimeSpan.Zero)
        {
            expiresIn = TimeSpan.FromMinutes(5);
        }

        var adapter = adapterName is null
            ? _adapterResolver.GetDefaultAdapter()
            : _adapterResolver.GetAdapter(adapterName);

        return GetSignedUrlInternalAsync(adapter, path, expiresIn, cancellationToken);
    }

    private async Task<SignedUrlResult> GetSignedUrlInternalAsync(
        IFileStorageAdapter adapter,
        string path,
        TimeSpan expiresIn,
        CancellationToken cancellationToken)
    {
        var url = await adapter.GetSignedUrlAsync(path, expiresIn, cancellationToken);
        return new SignedUrlResult(adapter.Name, url, DateTimeOffset.UtcNow.Add(expiresIn));
    }

    private static string GetFileExtension(string originalFileName, IImageFormat format)
    {
        var extension = Path.GetExtension(originalFileName);
        if (!string.IsNullOrWhiteSpace(extension))
        {
            return extension;
        }

        var fallbackExtension = format.FileExtensions.FirstOrDefault();
        return fallbackExtension is null ? ".img" : $".{fallbackExtension}";
    }
}
