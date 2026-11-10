using EthioFleet.Domain.Common;
using EthioFleet.Domain.Drivers;
using EthioFleet.Domain.Incidents;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Tests.UnitTests;

public sealed class IncidentDomainTests
{
    [Fact]
    public void IncidentResolvesWithActualCostAndCannotBeReopened()
    {
        var incident = CreateIncident();
        incident.UpdateStatus(IncidentStatus.Resolved, "Repair completed", 125.50m, DateTimeOffset.UtcNow);

        Assert.Equal(IncidentStatus.Resolved, incident.Status);
        Assert.Equal(125.50m, incident.ActualCost);
        Assert.Throws<DomainException>(() => incident.UpdateStatus(IncidentStatus.Open, null, null, DateTimeOffset.UtcNow));
    }

    [Fact]
    public void ResolvingIncidentRequiresNotesAndValidCost()
    {
        var incident = CreateIncident();
        Assert.Throws<DomainException>(() => incident.UpdateStatus(IncidentStatus.Resolved, " ", 0m, DateTimeOffset.UtcNow));
        Assert.Throws<DomainException>(() => incident.UpdateStatus(IncidentStatus.Resolved, "Fixed", -1m, DateTimeOffset.UtcNow));
        Assert.Equal(IncidentStatus.Open, incident.Status);
    }

    [Fact]
    public void IncidentRequiresVehicleAndDriverToShareTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var vehicle = CreateVehicle(tenantA);
        var driver = CreateDriver(tenantB);
        Assert.Throws<DomainException>(() => new Incident(tenantA, vehicle, driver, IncidentType.Accident,
            IncidentSeverity.High, DateTimeOffset.UtcNow, "Road 1", "Collision"));
    }

    private static Incident CreateIncident()
    {
        var tenant = Guid.NewGuid();
        return new Incident(tenant, CreateVehicle(tenant), null, IncidentType.Mechanical,
            IncidentSeverity.Medium, DateTimeOffset.UtcNow, "Depot", "Alternator fault", 300m);
    }

    private static Vehicle CreateVehicle(Guid tenant) => new(
        "INC-1", "Toyota", "Hilux", 2022, VehicleType.Pickup, FuelType.Diesel, 100m)
    {
        OrganizationId = tenant
    };

    private static Driver CreateDriver(Guid tenant)
    {
        var id = Guid.NewGuid();
        var license = new DriverLicense(id, "INC-LIC", "B", DateOnly.FromDateTime(DateTime.UtcNow.AddYears(1)));
        return new Driver(id, "Incident Driver", "incident@example.test", "+251911000000", license)
        {
            OrganizationId = tenant
        };
    }
}
