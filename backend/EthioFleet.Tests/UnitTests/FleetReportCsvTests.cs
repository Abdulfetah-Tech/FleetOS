using EthioFleet.Application.Reports;
using Xunit;

namespace EthioFleet.Tests.UnitTests;

public sealed class FleetReportCsvTests
{
    [Theory]
    [InlineData("=CMD()", "'=CMD()")]
    [InlineData("+123", "'+123")]
    [InlineData("-2+3", "'-2+3")]
    [InlineData("@SUM(A1)", "'@SUM(A1)")]
    public void CsvEscapesFormulaLikeFields(string input, string expected)
    {
        var report = new FleetReportResponse(
            DateOnly.FromDateTime(DateTime.UtcNow), DateOnly.FromDateTime(DateTime.UtcNow),
            [], [new VehicleCostRow(Guid.NewGuid(), input, "ETB", 1m, 2m, 1, 1)], []);
        var csv = System.Text.Encoding.UTF8.GetString(FleetReportCsv.Generate(report));
        Assert.Contains($"\"{expected}\"", csv, StringComparison.Ordinal);
    }
}
