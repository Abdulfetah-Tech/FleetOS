namespace EthioFleet.Application.Reports;

public sealed record FleetReportRequest(DateOnly From, DateOnly To);
public sealed record VehicleCostRow(Guid VehicleId, string PlateNumber, string CurrencyCode, decimal FuelCost, decimal MaintenanceCost, int FuelRecordCount, int MaintenanceRecordCount);
public sealed record CurrencyCostSummary(string CurrencyCode, decimal FuelCost, decimal MaintenanceCost, decimal TotalCost);
public sealed record VehicleUtilizationRow(Guid VehicleId, string PlateNumber, int CompletedTrips, decimal DistanceKm);
public sealed record FleetReportResponse(
    DateOnly From,
    DateOnly To,
    IReadOnlyCollection<CurrencyCostSummary> CostsByCurrency,
    IReadOnlyCollection<VehicleCostRow> VehicleCosts,
    IReadOnlyCollection<VehicleUtilizationRow> Utilization);

public interface IFleetReportService
{
    Task<FleetReportResponse> GetAsync(FleetReportRequest request, CancellationToken cancellationToken);
}

public static class FleetReportCsv
{
    public static byte[] Generate(FleetReportResponse report)
    {
        var builder = new System.Text.StringBuilder();
        builder.AppendLine("section,vehicle_id,plate_number,currency,fuel_cost,maintenance_cost,total_cost,fuel_records,maintenance_records,completed_trips,distance_km");
        foreach (var row in report.VehicleCosts)
        {
            builder.Append("vehicle-cost,");
            Append(builder, row.VehicleId.ToString());
            builder.Append(',');
            Append(builder, row.PlateNumber);
            builder.Append(',');
            Append(builder, row.CurrencyCode);
            builder.Append(',').Append(row.FuelCost.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(',').Append(row.MaintenanceCost.ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(',').Append((row.FuelCost + row.MaintenanceCost).ToString(System.Globalization.CultureInfo.InvariantCulture));
            builder.Append(',').Append(row.FuelRecordCount).Append(',').Append(row.MaintenanceRecordCount).AppendLine(",,");
        }

        foreach (var row in report.Utilization)
        {
            builder.Append("utilization,");
            Append(builder, row.VehicleId.ToString());
            builder.Append(',');
            Append(builder, row.PlateNumber);
            builder.Append(",,,,").Append(",,").Append(row.CompletedTrips).Append(',')
                .Append(row.DistanceKm.ToString(System.Globalization.CultureInfo.InvariantCulture)).AppendLine();
        }

        return System.Text.Encoding.UTF8.GetBytes(builder.ToString());
    }

    private static void Append(System.Text.StringBuilder builder, string value)
    {
        var safe = value.Length > 0 && value[0] is '=' or '+' or '-' or '@' or '\t' or '\r'
            ? "'" + value
            : value;
        builder.Append('"').Append(safe.Replace("\"", "\"\"", StringComparison.Ordinal)).Append('"');
    }
}
