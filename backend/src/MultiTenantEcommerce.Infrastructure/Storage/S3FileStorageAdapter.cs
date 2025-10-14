using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;
using MultiTenantEcommerce.Application.Models.Media;

namespace MultiTenantEcommerce.Infrastructure.Storage;

internal sealed class S3FileStorageAdapter : IFileStorageAdapter, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly S3FileStorageOptions _options;

    public S3FileStorageAdapter(IOptions<S3FileStorageOptions> options)
    {
        _options = options.Value;
        _client = CreateClient(_options);
    }

    public string Name => "s3";

    public async Task<StoredFileReference> SaveAsync(Stream content, string path, string contentType, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(path);
        EnsureBucketConfigured();

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = normalizedPath,
            InputStream = content,
            ContentType = contentType
        };

        await _client.PutObjectAsync(putRequest, cancellationToken);
        var url = await GetPublicUrlAsync(normalizedPath, cancellationToken);
        return new StoredFileReference(Name, normalizedPath, url);
    }

    public async Task DeleteAsync(string path, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            return;
        }

        var normalizedPath = NormalizePath(path);
        var deleteRequest = new DeleteObjectRequest
        {
            BucketName = _options.BucketName,
            Key = normalizedPath
        };

        await _client.DeleteObjectAsync(deleteRequest, cancellationToken);
    }

    public Task<string> GetPublicUrlAsync(string path, CancellationToken cancellationToken)
    {
        var normalizedPath = NormalizePath(path);

        if (!string.IsNullOrWhiteSpace(_options.CdnBaseUrl))
        {
            return Task.FromResult($"{_options.CdnBaseUrl!.TrimEnd('/')}/{normalizedPath}");
        }

        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            return Task.FromResult(normalizedPath);
        }

        var region = string.IsNullOrWhiteSpace(_options.Region) ? "us-east-1" : _options.Region!;
        return Task.FromResult($"https://{_options.BucketName}.s3.{region}.amazonaws.com/{normalizedPath}");
    }

    public Task<string> GetSignedUrlAsync(string path, TimeSpan expiresIn, CancellationToken cancellationToken)
    {
        EnsureBucketConfigured();
        var normalizedPath = NormalizePath(path);

        var preSignedRequest = new GetPreSignedUrlRequest
        {
            BucketName = _options.BucketName,
            Key = normalizedPath,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = HttpVerb.GET
        };

        var url = _client.GetPreSignedURL(preSignedRequest);
        return Task.FromResult(url);
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("A file path must be provided.", nameof(path));
        }

        return path.Replace('\\', '/').TrimStart('/');
    }

    private static IAmazonS3 CreateClient(S3FileStorageOptions options)
    {
        AmazonS3Config config;
        if (!string.IsNullOrWhiteSpace(options.ServiceUrl))
        {
            config = new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                ForcePathStyle = options.ForcePathStyle
            };
        }
        else
        {
            var region = string.IsNullOrWhiteSpace(options.Region)
                ? RegionEndpoint.USEast1
                : RegionEndpoint.GetBySystemName(options.Region);

            config = new AmazonS3Config
            {
                RegionEndpoint = region
            };
        }

        if (!string.IsNullOrWhiteSpace(options.AccessKey) && !string.IsNullOrWhiteSpace(options.SecretKey))
        {
            return new AmazonS3Client(options.AccessKey, options.SecretKey, config);
        }

        return new AmazonS3Client(config);
    }

    private void EnsureBucketConfigured()
    {
        if (string.IsNullOrWhiteSpace(_options.BucketName))
        {
            throw new InvalidOperationException("S3 bucket name is not configured.");
        }
    }
}
