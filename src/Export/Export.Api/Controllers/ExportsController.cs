using System.Diagnostics;
using Export.Core.Exports;
using Export.Core.Handlers;
using Microsoft.AspNetCore.Mvc;
using Shared.Logging;
using Shared.Models.Exports;

namespace Export.Api.Controllers;

[ApiController]
public class ExportsController(IExportHandler exportHandler, ILogger<ExportsController> logger) : ControllerBase
{
    [HttpPost("exports")]
    [ProducesResponseType<Stream>(StatusCodes.Status200OK, "application/zip")]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status504GatewayTimeout)]
    public async Task<IResult> Create(ExportRequest request, CancellationToken cancellationToken)
    {
        var started = Stopwatch.GetTimestamp();
        PreparedExport? export = null;
        logger.LogInformation("Start ExportsController.Create with params cabinet={Cabinet}, field={Field}, from={From}, to={To}",
            request.Cabinet, request.Field, request.From, request.To);
        try
        {
            export = await exportHandler.PrepareAsync(request.Cabinet, request.Field, request.From, request.To, cancellationToken);
            var prepared = export;
            return TypedResults.Stream(output => WriteAsync(prepared, output, cancellationToken), "application/zip", prepared.FileName);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error ExportsController.Create with params cabinet={Cabinet}, field={Field}, from={From}, to={To}",
                request.Cabinet, request.Field, request.From, request.To);
            throw;
        }
        finally
        {
            logger.LogInformation("End ExportsController.Create with params cabinet={Cabinet}, field={Field}, from={From}, to={To} in {ElapsedMs:0} ms; documents={DocumentCount}, fileName={FileName}",
                request.Cabinet, request.Field, request.From, request.To, Stopwatch.GetElapsedTime(started).TotalMilliseconds,
                export?.FirstPage.TotalCount, export?.FileName);
        }
    }

    // Once the first zip bytes are sent, a failure can only break the download; before that, the error handler answers with ProblemDetails.
    private async Task WriteAsync(PreparedExport export, Stream output, CancellationToken cancellationToken)
    {
        try
        {
            await exportHandler.WriteAsync(export, output, cancellationToken);
        }
        catch when (Response.HasStarted)
        {
            HttpContext.Abort();
        }
    }
}