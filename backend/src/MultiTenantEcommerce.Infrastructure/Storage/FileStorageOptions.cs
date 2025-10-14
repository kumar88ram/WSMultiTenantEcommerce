using System.IO;

namespace MultiTenantEcommerce.Infrastructure.Storage;

public class FileStorageOptions
{
    public string DefaultAdapter { get; set; } = "local";
}

public class LocalFileStorageOptions
{
    public string RootPath { get; set; } = Path.Combine("App_Data", "uploads");

    public string? PublicBaseUrl { get; set; }
        = "/uploads";

    public string? SignedUrlSecret { get; set; }
        = "change-me";
}

public class S3FileStorageOptions
{
    public string? BucketName { get; set; }
        = "";

    public string? Region { get; set; }
        = "us-east-1";

    public string? AccessKey { get; set; }
        = string.Empty;

    public string? SecretKey { get; set; }
        = string.Empty;

    public string? ServiceUrl { get; set; }
        = null;

    public bool ForcePathStyle { get; set; }
        = false;

    public string? CdnBaseUrl { get; set; }
        = null;
}
