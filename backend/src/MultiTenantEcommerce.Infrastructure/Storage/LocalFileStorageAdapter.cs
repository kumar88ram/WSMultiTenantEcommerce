using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Media;

namespace MultiTenantEcommerce.Infrastructure.Storage;

internal sealed class LocalFileStorageAdapter : IFileStorageAdapter
{
    private readonly IHostEnvironment _environment;
    private readonly IOptions<LocalFileStorageOptions> _options;

    public LocalFileStorageAdapter(IHostEnvironment environment, IOptions<LocalFileStorageOptions> options)
    {
        _environment = environment;
        _options = options;
    }

    public string Name => "local";

    public async Task<StoredFileReference> SaveAsync(Stream content, string path, string contentType, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(path);
        var fullPath = GetPhysicalPath(normalizedPath);

        var directory = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);
        await content.CopyToAsync(fileStream, cancellationToken);

        var url = await GetPublicUrlAsync(normalizedPath, cancellationToken);
        return new StoredFileReference(Name, normalizedPath, url);
    }

    public Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(path);
        var fullPath = GetPhysicalPath(normalizedPath);
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        return Task.CompletedTask;
    }

    public Task<string> GetPublicUrlAsync(string path, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(path);
        var baseUrl = _options.Value.PublicBaseUrl;

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            return Task.FromResult($"/{normalizedPath}");
        }

        return Task.FromResult($"{baseUrl.TrimEnd('/')}/{normalizedPath}");
    }

    public async Task<string> GetSignedUrlAsync(string path, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        var publicUrl = await GetPublicUrlAsync(path, cancellationToken);
        var secret = _options.Value.SignedUrlSecret;
        if (string.IsNullOrEmpty(secret))
        {
            return publicUrl;
        }

        var expiresAt = DateTimeOffset.UtcNow.Add(expiresIn);
        var payload = $"{NormalizePath(path)}:{expiresAt.ToUnixTimeSeconds()}";
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var signature = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(payload))).ToLowerInvariant();

        var separator = publicUrl.Contains('?', StringComparison.Ordinal) ? '&' : '?';
        return $"{publicUrl}{separator}expires={expiresAt.ToUnixTimeSeconds()}&signature={signature}";
    }

    private string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path must be provided.", nameof(path));
        }

        return path.Replace('\\', '/').TrimStart('/');
    }

    private string GetPhysicalPath(string normalizedPath)
    {
        var root = _options.Value.RootPath;
        var basePath = string.IsNullOrWhiteSpace(root)
            ? Path.Combine(_environment.ContentRootPath, "App_Data", "uploads")
            : Path.IsPathRooted(root)
                ? root
                : Path.Combine(_environment.ContentRootPath, root);

        return Path.Combine(basePath, normalizedPath.Replace('/', Path.DirectorySeparatorChar));
    }
}
