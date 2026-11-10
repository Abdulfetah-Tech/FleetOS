using Asp.Versioning;
using EthioFleet.Application.Reports;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/reports/fleet")]
[Authorize(Policy = "FinanceManager")]
public sealed class FleetReportsController(IFleetReportService reportService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<FleetReportResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FleetReportResponse>> Get(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            return Ok(await reportService.GetAsync(new FleetReportRequest(from, to), cancellationToken));
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("export.csv")]
    [Produces("text/csv")]
    public async Task<IActionResult> ExportCsv(
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken cancellationToken)
    {
        try
        {
            var report = await reportService.GetAsync(new FleetReportRequest(from, to), cancellationToken);
            var content = FleetReportCsv.Generate(report);
            return File(content, "text/csv; charset=utf-8", $"ethiofleet-report-{from:yyyyMMdd}-{to:yyyyMMdd}.csv");
        }
        catch (ArgumentOutOfRangeException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
