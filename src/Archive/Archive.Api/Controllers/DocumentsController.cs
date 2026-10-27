using Archive.Core.Handlers;
using Microsoft.AspNetCore.Mvc;

namespace Archive.Api.Controllers;

[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentHandler _documentHandler;

    public DocumentsController(IDocumentHandler documentHandler)
    {
        _documentHandler = documentHandler;
    }
}
