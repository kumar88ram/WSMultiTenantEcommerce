using System.IO.Compression;
using System.Text.Json;
using MultiTenantEcommerce.Application.Models;

namespace MultiTenantEcommerce.Application.Services;

public class ThemeBuilderService
{
    public async Task<ThemeManifest> ParseManifestAsync(Stream packageStream, CancellationToken cancellationToken = default)
    {
        if (!packageStream.CanSeek)
        {
            throw new InvalidOperationException("Theme package stream must be seekable.");
        }

        packageStream.Position = 0;
        using var archive = new ZipArchive(packageStream, ZipArchiveMode.Read, leaveOpen: true);
        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidOperationException("Theme package is missing manifest.json");

        await using var manifestStream = manifestEntry.Open();
        var manifest = await JsonSerializer.DeserializeAsync<ThemeManifest>(manifestStream, cancellationToken: cancellationToken);
        if (manifest is null)
        {
            throw new InvalidOperationException("Unable to parse manifest.json");
        }

        ValidateManifest(manifest);
        return manifest;
    }

    private static void ValidateManifest(ThemeManifest manifest)
    {
        if (string.IsNullOrWhiteSpace(manifest.Name))
        {
            throw new InvalidOperationException("Theme manifest must specify a name.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Code))
        {
            throw new InvalidOperationException("Theme manifest must specify a code.");
        }

        if (string.IsNullOrWhiteSpace(manifest.Version))
        {
            throw new InvalidOperationException("Theme manifest must specify a version.");
        }
    }
}
