using EthioFleet.Domain.Common;
using EthioFleet.Domain.Fuel;

namespace EthioFleet.Tests.UnitTests;

public sealed class FuelRecordDomainTests
{
    [Theory]
    [InlineData("12.5", "2.35", "29.38")]
    [InlineData("0.01", "0.50", "0.01")]
    [InlineData("2", "0", "0")]
    public void TotalCostUsesDecimalArithmeticAndRoundsToCents(string quantity, string unitPrice, string expected)
    {
        var parsedQuantity = decimal.Parse(quantity, System.Globalization.CultureInfo.InvariantCulture);
        var parsedUnitPrice = decimal.Parse(unitPrice, System.Globalization.CultureInfo.InvariantCulture);
        var parsedExpected = decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture);

        Assert.Equal(parsedExpected, FuelRecord.CalculateTotalCost(parsedQuantity, parsedUnitPrice));
    }

    [Fact]
    public void CostRejectsInvalidQuantityAndUnitPrice()
    {
        Assert.Throws<DomainException>(() => FuelRecord.CalculateTotalCost(0, 1));
        Assert.Throws<DomainException>(() => FuelRecord.CalculateTotalCost(1, -0.01m));
        Assert.Throws<DomainException>(() => FuelRecord.CalculateTotalCost(1.0001m, 1));
    }
}
