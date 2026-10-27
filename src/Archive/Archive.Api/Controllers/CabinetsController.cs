using Archive.Api.Mappers;
using Archive.Core.Cabinets;
using Microsoft.AspNetCore.Mvc;
using Shared.Models.Cabinets;

namespace Archive.Api.Controllers;

[ApiController]
[Route("cabinets")]
public class CabinetsController : ControllerBase
{
    private readonly CabinetCatalog _cabinets;

    public CabinetsController(CabinetCatalog cabinets)
    {
        _cabinets = cabinets;
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<CabinetDto>>(StatusCodes.Status200OK)]
    public IActionResult GetAll()
    {
        return Ok(_cabinets.All.Select(c => c.ToDto()).ToList());
    }

    [HttpGet("{cabinet}")]
    [ProducesResponseType<CabinetDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public IActionResult Get(string cabinet)
    {
        return Ok(_cabinets.Get(cabinet).ToDto());
    }
}
