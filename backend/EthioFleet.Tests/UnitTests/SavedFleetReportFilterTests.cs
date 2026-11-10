using EthioFleet.Domain.Common;
using EthioFleet.Domain.Reports;

namespace EthioFleet.Tests.UnitTests;

public sealed class SavedFleetReportFilterTests
{
    [Fact]
    public void CustomPresetRequiresValidDatesAndNormalizesCurrency()
    {
        var filter = new SavedFleetReportFilter(Guid.NewGuid(), Guid.NewGuid(), "  Monthly ETB ",
            new FleetReportFilterConfiguration(FleetReportDatePreset.Custom,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CurrencyCode: "etb"));

        Assert.Equal("Monthly ETB", filter.Name);
        Assert.Equal("ETB", filter.Configuration.CurrencyCode);
        Assert.Equal(new DateOnly(2026, 1, 31), filter.Configuration.CustomTo);
    }

    [Fact]
    public void PresetRejectsExplicitDatesAndInvalidCurrency()
    {
        Assert.Throws<DomainException>(() => new SavedFleetReportFilter(Guid.NewGuid(), Guid.NewGuid(), "Preset",
            new FleetReportFilterConfiguration(FleetReportDatePreset.Last30Days,
                new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 30))));
        Assert.Throws<DomainException>(() => new SavedFleetReportFilter(Guid.NewGuid(), Guid.NewGuid(), "Preset",
            new FleetReportFilterConfiguration(FleetReportDatePreset.ThisMonth, CurrencyCode: "US")));
    }
}
