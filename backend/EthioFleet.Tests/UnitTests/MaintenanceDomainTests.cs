using EthioFleet.Domain.Common;
using EthioFleet.Domain.Maintenance;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Tests.UnitTests;

public sealed class MaintenanceDomainTests
{
    [Fact]
    public void MaintenanceRecordCalculatesTotalCostUsingDecimalAmounts()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var record = new MaintenanceRecord(organizationId, vehicle, MaintenanceType.Preventive,
            "Oil and filter change", new DateOnly(2026, 5, 1), 1200m, 125.25m, 300.50m);

        Assert.Equal(425.75m, record.TotalCost);
        Assert.Equal("ETB", record.CurrencyCode);
    }

    [Fact]
    public void ScheduleIsDueWhenDateOrMileageThresholdIsReached()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var schedule = new MaintenanceSchedule(organizationId, vehicle, "Service", null,
            new DateOnly(2026, 6, 1), 1500m, 180, 300m);

        Assert.False(schedule.IsDue(new DateOnly(2026, 5, 31), 1499m));
        Assert.True(schedule.IsDue(new DateOnly(2026, 5, 31), 1500m));
        Assert.True(schedule.IsDue(new DateOnly(2026, 6, 1), 1200m));
    }

    [Fact]
    public void CompletingRecurringScheduleAdvancesBothDueThresholds()
    {
        var organizationId = Guid.NewGuid();
        var vehicle = CreateVehicle(organizationId);
        var schedule = new MaintenanceSchedule(organizationId, vehicle, "Service", null,
            new DateOnly(2026, 6, 1), 1500m, 180, 300m);
        schedule.Complete(new DateOnly(2026, 6, 3), 1525.5m);

        Assert.Equal(new DateOnly(2026, 11, 30), schedule.NextDueDate);
        Assert.Equal(1825.5m, schedule.NextDueMileage);
        Assert.False(schedule.IsDue(new DateOnly(2026, 6, 3), 1525.5m));
    }

    [Fact]
    public void NonRecurringCompletedScheduleBecomesCompleted()
    {
        var organizationId = Guid.NewGuid();
        var schedule = new MaintenanceSchedule(organizationId, CreateVehicle(organizationId), "One-off repair", null,
            new DateOnly(2026, 6, 1), null, null, null);
        schedule.Complete(new DateOnly(2026, 6, 1), 1200m);

        Assert.Equal(MaintenanceScheduleStatus.Completed, schedule.Status);
        Assert.False(schedule.IsDue(new DateOnly(2026, 7, 1), 1300m));
    }

    [Fact]
    public void ScheduleRequiresAtLeastOneDueThreshold()
    {
        var organizationId = Guid.NewGuid();
        Assert.Throws<DomainException>(() => new MaintenanceSchedule(organizationId, CreateVehicle(organizationId),
            "Service", null, null, null, null, null));
    }

    private static Vehicle CreateVehicle(Guid organizationId)
    {
        return new Vehicle("MAINT-1", "Toyota", "Hilux", 2022, VehicleType.Pickup, FuelType.Diesel, 100m)
        {
            OrganizationId = organizationId
        };
    }
}
