using EthioFleet.Domain.Common;
using EthioFleet.Domain.Fuel;

namespace EthioFleet.Tests.UnitTests;

public sealed class FuelRecordDomainTests
{
    [Theory]
    [InlineData(12.5, 2.35, 29.38)]
    [InlineData(0.01, 0.50, 0.01)]
    [InlineData(2, 0, 0)]
    public void TotalCostUsesDecimalArithmeticAndRoundsToCents(decimal quantity, decimal unitPrice, decimal expected)
    {
        Assert.Equal(expected, FuelRecord.CalculateTotalCost(quantity, unitPrice));
    }

    [Fact]
    public void CostRejectsInvalidQuantityAndUnitPrice()
    {
        Assert.Throws<DomainException>(() => FuelRecord.CalculateTotalCost(0, 1));
        Assert.Throws<DomainException>(() => FuelRecord.CalculateTotalCost(1, -0.01m));
        Assert.Throws<DomainException>(() => FuelRecord.CalculateTotalCost(1.0001m, 1));
    }
}
