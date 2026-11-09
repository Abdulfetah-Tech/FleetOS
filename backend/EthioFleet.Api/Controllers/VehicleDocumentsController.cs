using Asp.Versioning;
using EthioFleet.Application.Documents;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EthioFleet.Api.Controllers;

[ApiController]
[ApiVersion("1.0")]
[Authorize(Policy = "TenantMember")]
[Route("api/v{version:apiVersion}/vehicle-documents")]
public sealed class VehicleDocumentsController(IVehicleDocumentService documentService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<VehicleDocumentPage>(StatusCodes.Status200OK)]
    public async Task<ActionResult<VehicleDocumentPage>> List(
        [FromQuery] Guid vehicleId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default) =>
        Ok(await documentService.ListAsync(vehicleId, page, pageSize, cancellationToken));

    [HttpPost]
    [Authorize(Policy = "CompanyOwner")]
    [RequestSizeLimit(50L * 1024 * 1024)]
    [ProducesResponseType<VehicleDocumentResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDocumentResponse>> Upload(
        [FromForm] CreateVehicleDocumentRequest request,
        [FromForm] IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file is null || file.Length <= 0)
        {
            return BadRequest(new { error = "A non-empty file is required." });
        }

        await using var stream = file.OpenReadStream();
        var result = await documentService.UploadAsync(request, file.FileName, file.ContentType, stream, cancellationToken);
        if (result.NotFound)
        {
            return NotFound(new { error = result.Error });
        }
        if (!result.Succeeded)
        {
            return BadRequest(new { error = result.Error });
        }

        return CreatedAtAction(nameof(Get), new { id = result.Document!.Id, version = "1.0" }, result.Document);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<VehicleDocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<VehicleDocumentResponse>> Get(Guid id, CancellationToken cancellationToken)
    {
        var result = await documentService.GetAsync(id, cancellationToken);
        return result.Succeeded ? Ok(result.Document) : NotFound(new { error = result.Error });
    }

    [HttpGet("{id:guid}/content")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid id, CancellationToken cancellationToken)
    {
        var (content, document) = await documentService.OpenReadAsync(id, cancellationToken);
        if (content is null || document is null)
        {
            if (content is not null)
            {
                await content.DisposeAsync();
            }
            return NotFound();
        }

        Response.Headers.ContentDisposition = $"attachment; filename=\"{Uri.EscapeDataString(document.FileName)}\"";
        return File(content, document.MediaType);
    }

    [HttpDelete("{id:guid}")]
    [Authorize(Policy = "CompanyOwner")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Archive(Guid id, CancellationToken cancellationToken)
    {
        var archived = await documentService.ArchiveAsync(id, cancellationToken);
        return archived ? NoContent() : NotFound();
    }
}
