using Microsoft.Extensions.Options;
using MultiTenantEcommerce.Application.Abstractions;

namespace MultiTenantEcommerce.Infrastructure.Storage;

internal sealed class FileStorageAdapterResolver : IFileStorageAdapterResolver
{
    private readonly IReadOnlyDictionary<string, IFileStorageAdapter> _adapters;
    private readonly IOptions<FileStorageOptions> _options;

    public FileStorageAdapterResolver(
        IEnumerable<IFileStorageAdapter> adapters,
        IOptions<FileStorageOptions> options)
    {
        _adapters = adapters.ToDictionary(a => a.Name, StringComparer.OrdinalIgnoreCase);
        _options = options;
    }

    public IFileStorageAdapter GetDefaultAdapter()
        => GetAdapter(_options.Value.DefaultAdapter);

    public IFileStorageAdapter GetAdapter(string? name)
    {
        var key = string.IsNullOrWhiteSpace(name)
            ? _options.Value.DefaultAdapter
            : name;

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new InvalidOperationException("A file storage adapter name must be provided.");
        }

        if (_adapters.TryGetValue(key, out var adapter))
        {
            return adapter;
        }

        throw new InvalidOperationException($"File storage adapter '{key}' is not registered.");
    }
}
