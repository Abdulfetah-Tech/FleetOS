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

    [HttpGet("saved-filters")]
    [ProducesResponseType<IReadOnlyCollection<SavedFleetReportFilterResponse>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> ListSavedFilters(CancellationToken cancellationToken) =>
        Ok(await reportService.ListSavedFiltersAsync(cancellationToken));

    [HttpPost("saved-filters")]
    [ProducesResponseType<SavedFleetReportFilterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateSavedFilter(
        CreateSavedFleetReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        var filter = await reportService.CreateSavedFilterAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetSavedFilter), new { id = filter.Id, version = "1.0" }, filter);
    }

    [HttpGet("saved-filters/{id:guid}")]
    [ProducesResponseType<SavedFleetReportFilterResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSavedFilter(Guid id, CancellationToken cancellationToken)
    {
        var filter = await reportService.GetSavedFilterAsync(id, cancellationToken);
        return filter is null ? NotFound() : Ok(filter);
    }

    [HttpPut("saved-filters/{id:guid}")]
    [ProducesResponseType<SavedFleetReportFilterResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSavedFilter(
        Guid id,
        UpdateSavedFleetReportFilterRequest request,
        CancellationToken cancellationToken)
    {
        var filter = await reportService.UpdateSavedFilterAsync(id, request, cancellationToken);
        return filter is null ? NotFound() : Ok(filter);
    }

    [HttpDelete("saved-filters/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSavedFilter(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await reportService.DeleteSavedFilterAsync(id, cancellationToken);
        return deleted ? NoContent() : NotFound();
    }
}
