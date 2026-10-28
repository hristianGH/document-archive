using Archive.Api.Cabinets;
using Archive.Core.Handlers;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Cabinets;

namespace Archive.Api.Controllers;

[ApiController]
[Route("cabinets")]
public class CabinetsController(ICabinetHandler cabinetHandler) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CabinetResponse>>(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(cabinetHandler.GetAll().Select(c => c.ToResponse()).ToList());
    }

    [HttpGet("{cabinet}")]
    [ProducesResponseType<CabinetResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Get(string cabinet)
    {
        return Ok(cabinetHandler.Get(cabinet).ToResponse());
    }
}
