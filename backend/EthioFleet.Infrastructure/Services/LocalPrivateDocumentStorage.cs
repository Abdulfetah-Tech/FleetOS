using EthioFleet.Application.Documents;
using System.Security.Cryptography;
using Microsoft.Extensions.Configuration;

namespace EthioFleet.Infrastructure.Services;

public sealed class LocalPrivateDocumentStorage(IConfiguration configuration) : IPrivateDocumentStorage
{
    private const int BufferSize = 81920;
    private readonly string _root = Path.GetFullPath(
        string.IsNullOrWhiteSpace(configuration["DocumentStorage:RootPath"])
            ? Path.Combine(AppContext.BaseDirectory, "private-documents")
            : configuration["DocumentStorage:RootPath"]!);

    public async Task<StoredDocument> StoreAsync(
        Guid organizationId,
        Guid documentId,
        Stream content,
        long maxBytes,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(_root);
        var key = $"{organizationId:N}/{documentId:N}";
        var path = ResolvePath(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        await using var destination = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, BufferSize, FileOptions.Asynchronous);
        using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[BufferSize];
        long size = 0;
        int read;
        while ((read = await content.ReadAsync(buffer, cancellationToken)) > 0)
        {
            size += read;
            if (size > maxBytes)
            {
                throw new InvalidDataException("Document exceeds the configured size limit.");
            }
            hash.AppendData(buffer, 0, read);
            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        if (size == 0)
        {
            throw new InvalidDataException("Document cannot be empty.");
        }

        return new StoredDocument(key, size, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            return Task.FromResult<Stream?>(null);
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, BufferSize, FileOptions.Asynchronous);
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || Path.IsPathRooted(storageKey))
        {
            throw new InvalidDataException("Storage key is invalid.");
        }
        var path = Path.GetFullPath(Path.Combine(_root, storageKey.Replace('/', Path.DirectorySeparatorChar)));
        if (!path.StartsWith(_root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Storage key resolves outside the private storage root.");
        }
        return path;
    }
}
