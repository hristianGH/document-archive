using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using Archive.Api.Documents;
using Archive.Api.Uploads;
using Archive.Core.Dates;
using Archive.Core.Handlers;
using Microsoft.AspNetCore.Mvc;
using Shared.Logging;
using Shared.Models.Documents;
using Shared.Models.Paging;

namespace Archive.Api.Controllers;

[ApiController]
public class DocumentsController(IDocumentHandler documentHandler, IUploadHandler uploadHandler, ILogger<DocumentsController> logger) : ControllerBase
{
    [HttpPost("cabinets/{cabinet}/uploads")]
    [Consumes("multipart/form-data")]
    [TypeFilter<UploadSizeLimitFilter>]
    [ProducesResponseType<UploadResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Upload(
        string cabinet,
        [FromForm, Description("A writable text field of the cabinet; every stored document gets the folder name in it.")] string field,
        [Description("The folder as a zip.")] IFormFile file,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        UploadResponse? response = null;
        logger.LogInformation("Start DocumentsController.Upload with params cabinet={Cabinet}, field={Field}, fileName={FileName}, fileBytes={FileBytes}",
            cabinet, field, file.FileName, file.Length);
        try
        {
            // Owns the upload stream and closes it; the handler's ZipArchive opens it with leaveOpen: true.
            await using var zip = file.OpenReadStream();
            var report = await uploadHandler.UploadAsync(cabinet, field, file.FileName, zip, cancellationToken);
            response = report.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentsController.Upload with params cabinet={Cabinet}, field={Field}, fileName={FileName}, fileBytes={FileBytes}",
                cabinet, field, file.FileName, file.Length);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentsController.Upload with params cabinet={Cabinet}, field={Field}, fileName={FileName}, fileBytes={FileBytes} in {ElapsedMs:0} ms; stored={StoredCount}, skipped={SkippedCount}, failed={FailedCount}",
                cabinet, field, file.FileName, file.Length, Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                response?.Stored.Count, response?.Skipped.Count, response?.Failed.Count);
        }
    }

    [HttpGet("cabinets/{cabinet}/documents")]
    [ProducesResponseType<PagedResponse<DocumentListItemResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPage(
        string cabinet,
        [FromQuery, Description(UtcInstant.FromDescription)] string from,
        [FromQuery, Description(UtcInstant.ToDescription)] string to,
        [FromQuery, Range(1, int.MaxValue)] int page = 1,
        [FromQuery, Range(1, int.MaxValue), Description("Capped at the configured maximum page size; the response states the size used.")] int pageSize = 5,
        CancellationToken cancellationToken = default)
    {
        var started = Stopwatch.GetTimestamp();
        PagedResponse<DocumentListItemResponse>? response = null;
        logger.LogInformation("Start DocumentsController.GetPage with params cabinet={Cabinet}, from={From}, to={To}, page={Page}, pageSize={PageSize}",
            cabinet, from, to, page, pageSize);
        try
        {
            var result = await documentHandler.GetPageAsync(cabinet, from, to, page, pageSize, cancellationToken);
            response = result.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentsController.GetPage with params cabinet={Cabinet}, from={From}, to={To}, page={Page}, pageSize={PageSize}",
                cabinet, from, to, page, pageSize);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentsController.GetPage with params cabinet={Cabinet}, from={From}, to={To}, page={Page}, pageSize={PageSize} in {ElapsedMs:0} ms; items={ItemCount}, total={TotalCount}",
                cabinet, from, to, page, pageSize, Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                response?.Items.Count, response?.TotalCount);
        }
    }

    [HttpGet("documents/{id:guid}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> Get(Guid id, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        DocumentResponse? response = null;
        logger.LogInformation("Start DocumentsController.Get with params id={Id}", id);
        try
        {
            var document = await documentHandler.GetAsync(id, cancellationToken);
            response = document.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentsController.Get with params id={Id}", id);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentsController.Get with params id={Id} in {ElapsedMs:0} ms; parts={PartCount}",
                id, Stopwatch.GetElapsedTime(started).TotalMilliseconds, response?.Parts.Count);
        }
    }

    [HttpGet("documents/{id:guid}/parts/{partId:guid}/content")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/octet-stream")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetPartContent(Guid id, Guid partId, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        string? fileName = null;
        logger.LogInformation("Start DocumentsController.GetPartContent with params id={Id}, partId={PartId}", id, partId);
        try
        {
            var part = await documentHandler.GetPartContentAsync(id, partId, cancellationToken);
            fileName = part.FileName;
            return File(part.Content, "application/octet-stream", part.FileName);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentsController.GetPartContent with params id={Id}, partId={PartId}", id, partId);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentsController.GetPartContent with params id={Id}, partId={PartId} in {ElapsedMs:0} ms; fileName={FileName}",
                id, partId, Stopwatch.GetElapsedTime(started).TotalMilliseconds, fileName);
        }
    }

    [HttpPut("documents/{id:guid}/index-values/{field}")]
    [ProducesResponseType<DocumentResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> SetIndexValue(
        Guid id,
        [Description("A writable field of the document's cabinet; case is ignored.")] string field,
        SetIndexValueRequest request,
        CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        DocumentResponse? response = null;
        logger.LogInformation("Start DocumentsController.SetIndexValue with params id={Id}, field={Field}", id, field);
        try
        {
            var document = await documentHandler.SetIndexValueAsync(id, field, request.Value, cancellationToken);
            response = document.ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error DocumentsController.SetIndexValue with params id={Id}, field={Field}", id, field);
            throw;
        }
        finally
        {
            logger.LogInformation("End DocumentsController.SetIndexValue with params id={Id}, field={Field} in {ElapsedMs:0} ms; modifiedAt={ModifiedAt:O}",
                id, field, Stopwatch.GetElapsedTime(started).TotalMilliseconds, response?.ModifiedAt);
        }
    }
}