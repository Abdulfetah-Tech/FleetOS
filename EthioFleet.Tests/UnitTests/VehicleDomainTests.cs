using EthioFleet.Domain.Common;
using EthioFleet.Domain.Vehicles;

namespace EthioFleet.Tests.UnitTests;

public sealed class VehicleDomainTests
{
    [Fact]
    public void ConstructorNormalizesIdentifiersAndSetsInitialState()
    {
        var vehicle = CreateVehicle(" ab-123 cd ", "1HGCM82633A004352");

        Assert.Equal("AB-123 CD", vehicle.PlateNumber);
        Assert.Equal("AB123CD", vehicle.NormalizedPlateNumber);
        Assert.Equal("1HGCM82633A004352", vehicle.Vin);
        Assert.Equal("1HGCM82633A004352", vehicle.NormalizedVin);
        Assert.Equal(VehicleStatus.Active, vehicle.Status);
        Assert.Equal("ETB", vehicle.CurrencyCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ConstructorRejectsMissingPlateNumber(string plate)
    {
        Assert.Throws<DomainException>(() => new Vehicle(plate, "Toyota", "Hilux", 2020,
            VehicleType.Pickup, FuelType.Diesel, 0));
    }

    [Theory]
    [InlineData("1HGCM82633A00435I")]
    [InlineData("1HGCM82633A00435O")]
    [InlineData("short")]
    public void ConstructorRejectsInvalidVin(string vin)
    {
        Assert.Throws<DomainException>(() => CreateVehicle("AB-123", vin));
    }

    [Fact]
    public void MileageCannotDecreaseOrHaveMoreThanOneFractionalDigit()
    {
        var vehicle = CreateVehicle("AB-123", null);
        vehicle.RecordOdometer(100.5m);

        Assert.Throws<DomainException>(() => vehicle.RecordOdometer(100));
        Assert.Throws<DomainException>(() => vehicle.RecordOdometer(101.25m));
        Assert.Equal(100.5m, vehicle.CurrentMileage);
    }

    [Fact]
    public void RetiredVehicleCannotReturnToAnOperationalStatus()
    {
        var vehicle = CreateVehicle("AB-123", null);
        vehicle.ChangeStatus(VehicleStatus.Retired);

        Assert.Throws<DomainException>(() => vehicle.ChangeStatus(VehicleStatus.Active));
        Assert.Equal(VehicleStatus.Retired, vehicle.Status);
    }

    [Fact]
    public void IdentifierNormalizationIgnoresSpacesAndHyphens()
    {
        Assert.Equal("AB123CD", Vehicle.NormalizeIdentifier("ab - 123 cd"));
    }

    private static Vehicle CreateVehicle(string plate, string? vin) => new(
        plate, "Toyota", "Hilux", 2020, VehicleType.Pickup, FuelType.Diesel, 0, vin);
}
