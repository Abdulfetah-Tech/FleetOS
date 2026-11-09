using EthioFleet.Application.Documents;
using EthioFleet.Domain.Documents;
using EthioFleet.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace EthioFleet.Infrastructure.Services;

public sealed class VehicleDocumentService(
    AppDbContext dbContext,
    IPrivateDocumentStorage storage,
    EthioFleet.Application.Interfaces.ITenantContext tenantContext,
    EthioFleet.Application.Interfaces.IClock clock) : IVehicleDocumentService
{
    private const long MaxFileSize = 50L * 1024 * 1024;

    public async Task<VehicleDocumentResult> UploadAsync(
        CreateVehicleDocumentRequest request,
        string fileName,
        string mediaType,
        Stream content,
        CancellationToken cancellationToken)
    {
        var vehicle = await dbContext.Vehicles.SingleOrDefaultAsync(item => item.Id == request.VehicleId, cancellationToken);
        if (vehicle is null)
        {
            return VehicleDocumentResult.Failure("Vehicle was not found.", notFound: true);
        }

        if (tenantContext.OrganizationId is not Guid organizationId || organizationId != vehicle.OrganizationId)
        {
            return VehicleDocumentResult.Failure("Vehicle was not found.", notFound: true);
        }

        var documentId = Guid.NewGuid();
        var stored = await storage.StoreAsync(organizationId, documentId, content, MaxFileSize, cancellationToken);
        try
        {
            var document = new VehicleDocument(organizationId, vehicle, request.Type, request.Name,
                fileName, mediaType, stored.StorageKey, stored.SizeBytes, request.IssuedOn, request.ExpiresOn, stored.Sha256)
            {
                Id = documentId
            };
            dbContext.VehicleDocuments.Add(document);
            await dbContext.SaveChangesAsync(cancellationToken);
            return VehicleDocumentResult.Success(ToResponse(document, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
        }
        catch
        {
            await storage.DeleteAsync(stored.StorageKey, CancellationToken.None);
            throw;
        }
    }

    public async Task<VehicleDocumentPage> ListAsync(Guid vehicleId, int page, int pageSize, CancellationToken cancellationToken)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);
        var documents = dbContext.VehicleDocuments.AsNoTracking().Where(document => document.VehicleId == vehicleId);
        var count = await documents.CountAsync(cancellationToken);
        var items = await documents.OrderBy(document => document.ExpiresOn)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
        var today = DateOnly.FromDateTime(clock.UtcNow.UtcDateTime);
        return new VehicleDocumentPage(items.Select(document => ToResponse(document, today)).ToArray(), page, pageSize, count);
    }

    public async Task<VehicleDocumentResult> GetAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.VehicleDocuments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);
        return document is null
            ? VehicleDocumentResult.Failure("Vehicle document was not found.", notFound: true)
            : VehicleDocumentResult.Success(ToResponse(document, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
    }

    public async Task<(Stream? Content, VehicleDocumentResponse? Document)> OpenReadAsync(
        Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.VehicleDocuments.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);
        if (document is null)
        {
            return (null, null);
        }

        var content = await storage.OpenReadAsync(document.StorageKey, cancellationToken);
        return (content, ToResponse(document, DateOnly.FromDateTime(clock.UtcNow.UtcDateTime)));
    }

    public async Task<bool> ArchiveAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await dbContext.VehicleDocuments
            .SingleOrDefaultAsync(item => item.Id == documentId, cancellationToken);
        if (document is null)
        {
            return false;
        }

        document.Archive();
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static VehicleDocumentResponse ToResponse(VehicleDocument document, DateOnly today)
    {
        var status = document.Status == VehicleDocumentStatus.Archived
            ? VehicleDocumentStatus.Archived
            : document.IsExpired(today) ? VehicleDocumentStatus.Expired : VehicleDocumentStatus.Active;
        return new VehicleDocumentResponse(document.Id, document.OrganizationId, document.VehicleId,
            document.Type, document.Name, document.FileName, document.MediaType, document.FileSizeBytes,
            document.IssuedOn, document.ExpiresOn, status, document.IsExpired(today),
            string.IsNullOrEmpty(document.ChecksumSha256) ? null : document.ChecksumSha256, document.CreatedAt);
    }
}
