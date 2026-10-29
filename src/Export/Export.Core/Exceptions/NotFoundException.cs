using System.Net;

namespace Export.Core.Exceptions;

public class NotFoundException : ExportException
{
    public NotFoundException(string message)
        : base(HttpStatusCode.NotFound, message)
    {
    }
}