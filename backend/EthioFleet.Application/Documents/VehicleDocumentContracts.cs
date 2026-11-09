using EthioFleet.Domain.Documents;

namespace EthioFleet.Application.Documents;

public sealed record CreateVehicleDocumentRequest(
    Guid VehicleId,
    VehicleDocumentType Type,
    string Name,
    DateOnly? IssuedOn = null,
    DateOnly? ExpiresOn = null);

public sealed record VehicleDocumentResponse(
    Guid Id,
    Guid OrganizationId,
    Guid VehicleId,
    VehicleDocumentType Type,
    string Name,
    string FileName,
    string MediaType,
    long FileSizeBytes,
    DateOnly? IssuedOn,
    DateOnly? ExpiresOn,
    VehicleDocumentStatus Status,
    bool IsExpired,
    string? ChecksumSha256,
    DateTimeOffset CreatedAt);

public sealed record VehicleDocumentResult(bool Succeeded, VehicleDocumentResponse? Document, string? Error, bool NotFound = false)
{
    public static VehicleDocumentResult Success(VehicleDocumentResponse document) => new(true, document, null);
    public static VehicleDocumentResult Failure(string error, bool notFound = false) => new(false, null, error, notFound);
}

public sealed record VehicleDocumentPage(IReadOnlyCollection<VehicleDocumentResponse> Items, int Page, int PageSize, int TotalCount);

public sealed record StoredDocument(string StorageKey, long SizeBytes, string Sha256);

public interface IPrivateDocumentStorage
{
    Task<StoredDocument> StoreAsync(Guid organizationId, Guid documentId, Stream content, long maxBytes, CancellationToken cancellationToken);
    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public interface IVehicleDocumentService
{
    Task<VehicleDocumentResult> UploadAsync(CreateVehicleDocumentRequest request, string fileName, string mediaType, Stream content, CancellationToken cancellationToken);
    Task<VehicleDocumentPage> ListAsync(Guid vehicleId, int page, int pageSize, CancellationToken cancellationToken);
    Task<VehicleDocumentResult> GetAsync(Guid documentId, CancellationToken cancellationToken);
    Task<(Stream? Content, VehicleDocumentResponse? Document)> OpenReadAsync(Guid documentId, CancellationToken cancellationToken);
    Task<bool> ArchiveAsync(Guid documentId, CancellationToken cancellationToken);
}
