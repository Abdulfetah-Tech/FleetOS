using EthioFleet.Domain.Common;
using EthioFleet.Domain.Documents;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Tests.UnitTests;

public sealed class VehicleDocumentDomainTests
{
    [Fact]
    public void DocumentExpiryAndArchiveStatesAreDeterministic()
    {
        var document = CreateDocument();
        Assert.False(document.IsExpired(new DateOnly(2026, 5, 31)));
        Assert.True(document.IsExpired(new DateOnly(2026, 6, 2)));
        document.RefreshExpiryStatus(new DateOnly(2026, 6, 2));
        Assert.Equal(VehicleDocumentStatus.Expired, document.Status);
        document.Archive();
        document.RefreshExpiryStatus(new DateOnly(2026, 5, 1));
        Assert.Equal(VehicleDocumentStatus.Archived, document.Status);
    }

    [Fact]
    public void DocumentRejectsInvalidDateRangeAndExternalStorageKeys()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        Assert.Throws<DomainException>(() => new VehicleDocument(organizationId, vehicle,
            VehicleDocumentType.Insurance, "Policy", "policy.pdf", "application/pdf", "key/1", 10,
            new DateOnly(2026, 6, 2), new DateOnly(2026, 6, 1)));
        Assert.Throws<DomainException>(() => new VehicleDocument(organizationId, vehicle,
            VehicleDocumentType.Insurance, "Policy", "policy.pdf", "application/pdf", "../outside", 10));
        Assert.Throws<DomainException>(() => new VehicleDocument(organizationId, vehicle,
            VehicleDocumentType.Insurance, "Policy", "policy.pdf", "application/pdf", "https://public.example/file", 10));
    }

    [Fact]
    public void FilenameIsReducedToSafeLeafNameAndChecksumRequiresSha256Hex()
    {
        var organizationId = Guid.NewGuid();
        var document = new VehicleDocument(organizationId, CreateVehicle(organizationId), VehicleDocumentType.Permit,
            "Road permit", "folder\\permit.pdf", "application/pdf", "tenant/document", 100,
            checksumSha256: new string('a', 64));

        Assert.Equal("permit.pdf", document.FileName);
        Assert.Equal(64, document.ChecksumSha256.Length);
        Assert.Throws<DomainException>(() => new VehicleDocument(organizationId, CreateVehicle(organizationId),
            VehicleDocumentType.Permit, "Road permit", "permit.pdf", "application/pdf", "tenant/document", 100,
            checksumSha256: "not-a-checksum"));
    }

    private static VehicleDocument CreateDocument()
    {
        var organizationId = Guid.NewGuid();
        return new VehicleDocument(organizationId, CreateVehicle(organizationId), VehicleDocumentType.Insurance,
            "Insurance Policy", "policy.pdf", "application/pdf", $"{organizationId:N}/document", 1024,
            new DateOnly(2025, 6, 1), new DateOnly(2026, 6, 1));
    }

    private static Vehicle CreateVehicle(Guid organizationId)
    {
        return new Vehicle("DOC-1", "Toyota", "Hilux", 2022, VehicleType.Pickup, FuelType.Diesel, 100m)
        {
            OrganizationId = organizationId
        };
    }
}
