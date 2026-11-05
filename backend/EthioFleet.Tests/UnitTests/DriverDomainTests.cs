using EthioFleet.Domain.Common;
using EthioFleet.Domain.Drivers;

namespace EthioFleet.Tests.UnitTests;

public sealed class DriverDomainTests
{
    [Fact]
    public void DriverNormalizesIdentityAndLicenseFields()
    {
        var driver = CreateDriver("  Hana Bekele ", "HANA@example.test", " +251 911 123456 ", " ab-12 34 ");

        Assert.Equal("Hana Bekele", driver.FullName);
        Assert.Equal("hana@example.test", driver.Email);
        Assert.Equal("HANA@EXAMPLE.TEST", driver.NormalizedEmail);
        Assert.Equal("+251 911 123456", driver.PhoneNumber);
        Assert.Equal("AB-12 34", driver.License.Number);
        Assert.Equal("AB1234", driver.License.NormalizedNumber);
        Assert.Equal(DriverStatus.Active, driver.Status);
    }

    [Fact]
    public void LicenseExpiryIsComparedToSuppliedDate()
    {
        var license = new DriverLicense(Guid.NewGuid(), "AB1234", "B", new DateOnly(2027, 5, 1));

        Assert.False(license.IsExpired(new DateOnly(2027, 5, 1)));
        Assert.True(license.IsExpired(new DateOnly(2027, 5, 2)));
    }

    [Theory]
    [InlineData("not-an-email")]
    [InlineData("")]
    public void DriverRejectsInvalidEmail(string email)
    {
        Assert.Throws<DomainException>(() => new Driver("Driver Name", email, "+251911000000",
            new DriverLicense(Guid.NewGuid(), "AB1234", "B", new DateOnly(2027, 1, 1))));
    }

    [Fact]
    public void DriverRejectsInvalidStatus()
    {
        var driver = CreateDriver("Driver Name", "driver@example.test", "+251911000000", "AB1234");

        Assert.Throws<DomainException>(() => driver.ChangeStatus((DriverStatus)999));
    }

    [Fact]
    public void LicenseUpdateDoesNotPartiallyMutateWhenExpiryIsInvalid()
    {
        var license = new DriverLicense(Guid.NewGuid(), "AB1234", "B", new DateOnly(2027, 1, 1));

        Assert.Throws<DomainException>(() => license.Update("CD5678", "C", default));
        Assert.Equal("AB1234", license.Number);
        Assert.Equal("B", license.Category);
    }

    private static Driver CreateDriver(string name, string email, string phone, string license) =>
        new(name, email, phone,
            new DriverLicense(Guid.NewGuid(), license, "B", new DateOnly(2027, 1, 1)));
}
