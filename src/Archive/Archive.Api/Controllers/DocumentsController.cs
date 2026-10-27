using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Archive.Api.Documents;
using Archive.Core.Dates;
using Archive.Core.Handlers;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Documents;
using Shared.Models.Paging;

namespace Archive.Api.Controllers;

[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentHandler _documentHandler;

    public DocumentsController(IDocumentHandler documentHandler)
    {
        _documentHandler = documentHandler;
    }

    [HttpGet("cabinets/{cabinet}/documents")]
    [ProducesResponseType<PagedResponse<DocumentListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPage(
        string cabinet,
        [FromQuery, Description("Inclusive. " + UtcInstant.Description)] string from,
        [FromQuery, Description("Exclusive. " + UtcInstant.Description)] string to,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, int.MaxValue), Description("Capped at the configured maximum page size; the response states the size used.")] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var result = await _documentHandler.GetPageAsync(cabinet, from, to, page, pageSize, cancellationToken);
        return Ok(result.ToResponse());
    }

    [HttpGet("documents/{id:guid}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var document = await _documentHandler.GetAsync(id, cancellationToken);
        return Ok(document.ToResponse());
    }

    [HttpGet("documents/{id:guid}/parts/{partId:guid}/content")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPartContent(Guid id, Guid partId, CancellationToken cancellationToken)
    {
        var part = await _documentHandler.GetPartContentAsync(id, partId, cancellationToken);
        return File(part.Content, "application/octet-stream", part.FileName);
    }
}
