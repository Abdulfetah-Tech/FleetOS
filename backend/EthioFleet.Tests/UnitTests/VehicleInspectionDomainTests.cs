using EthioFleet.Domain.Common;
using EthioFleet.Domain.Inspections;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Tests.UnitTests;

public sealed class VehicleInspectionDomainTests
{
    [Fact]
    public void CompletingChecklistSetsPassAndNoOutOfServiceFlagWhenAllItemsPass()
    {
        var inspection = CreateInspection();
        inspection.Complete(
        [
            new("tires", InspectionResult.Pass, null),
            new("brakes", InspectionResult.Pass, "No issue")
        ], DateTimeOffset.UtcNow);

        Assert.Equal(InspectionStatus.Passed, inspection.Status);
        Assert.False(inspection.RequiresVehicleOutOfService);
        Assert.Equal(2, inspection.Responses.Count);
    }

    [Fact]
    public void CriticalFailureRequiresVehicleOutOfService()
    {
        var inspection = CreateInspection();
        inspection.Complete(
        [
            new("tires", InspectionResult.Pass, null),
            new("brakes", InspectionResult.Fail, "Brake warning light")
        ], DateTimeOffset.UtcNow);

        Assert.Equal(InspectionStatus.Failed, inspection.Status);
        Assert.True(inspection.RequiresVehicleOutOfService);
    }

    [Fact]
    public void MissingOrDuplicateChecklistResultsAreRejected()
    {
        var inspection = CreateInspection();
        Assert.Throws<DomainException>(() => inspection.Complete(
            [new("tires", InspectionResult.Pass, null)], DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => inspection.Complete(
        [
            new("tires", InspectionResult.Pass, null),
            new("tires", InspectionResult.Pass, null)
        ], DateTimeOffset.UtcNow));
    }

    [Fact]
    public void UnknownChecklistItemIsRejectedAndInspectionRemainsInProgress()
    {
        var inspection = CreateInspection();
        Assert.Throws<DomainException>(() => inspection.Complete(
        [
            new("tires", InspectionResult.Pass, null),
            new("unknown", InspectionResult.Pass, null)
        ], DateTimeOffset.UtcNow));
        Assert.Equal(InspectionStatus.InProgress, inspection.Status);
    }

    private static VehicleInspection CreateInspection()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = new Vehicle("INSP-1", "Toyota", "Hilux", 2022,
            VehicleType.Pickup, FuelType.Diesel, 100m) { OrganizationId = organizationId };
        var checklist = new InspectionChecklistDefinition("v1",
        [
            new("tires", "Tires", false),
            new("brakes", "Brakes", true)
        ]);
        return new VehicleInspection(organizationId, vehicle, DateTimeOffset.UtcNow, 100m, null, checklist);
    }
}
