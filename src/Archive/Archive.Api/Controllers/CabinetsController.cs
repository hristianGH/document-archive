using System.Diagnostics;
using Archive.Api.Cabinets;
using Archive.Core.Handlers;
using Archive.Core.Logging;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Cabinets;

namespace Archive.Api.Controllers;

[ApiController]
[Route("cabinets")]
public class CabinetsController(ICabinetHandler cabinetHandler, ILogger<CabinetsController> logger) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CabinetResponse>>(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        var started = Stopwatch.GetTimestamp();
        List<CabinetResponse>? cabinets = null;
        logger.LogInformation("Start CabinetsController.GetAll");
        try
        {
            cabinets = cabinetHandler.GetAll().Select(c => c.ToResponse()).ToList();
            return Ok(cabinets);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error CabinetsController.GetAll");
            throw;
        }
        finally
        {
            logger.LogInformation("End CabinetsController.GetAll in {ElapsedMs:0} ms; cabinets={CabinetCount}",
                Stopwatch.GetElapsedTime(started).TotalMilliseconds, cabinets?.Count);
        }
    }

    [HttpGet("{cabinet}")]
    [ProducesResponseType<CabinetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Get(string cabinet)
    {
        var started = Stopwatch.GetTimestamp();
        CabinetResponse? response = null;
        logger.LogInformation("Start CabinetsController.Get with params cabinet={Cabinet}", cabinet);
        try
        {
            response = cabinetHandler.Get(cabinet).ToResponse();
            return Ok(response);
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error CabinetsController.Get with params cabinet={Cabinet}", cabinet);
            throw;
        }
        finally
        {
            logger.LogInformation("End CabinetsController.Get with params cabinet={Cabinet} in {ElapsedMs:0} ms; fields={FieldCount}",
                cabinet, Stopwatch.GetElapsedTime(started).TotalMilliseconds, response?.Fields.Count);
        }
    }
}
