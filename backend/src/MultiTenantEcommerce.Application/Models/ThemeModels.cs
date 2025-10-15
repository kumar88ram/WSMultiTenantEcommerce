using System.IO;

namespace MultiTenantEcommerce.Application.Models;

public record ThemeUploadContext(
    string FileName,
    Stream Content,
    string StorageRoot,
    ThemeManifest Manifest);

public record ThemeManifest(
    string Name,
    string Code,
    string Version,
    string? Description,
    string? PreviewImageUrl);

public record ThemeSectionDefinition(
    string SectionName,
    string JsonConfig,
    int SortOrder);

public record ThemeVariableValue(
    string Key,
    string Value);
