using System.Net;

namespace Archive.Core.Exceptions;

public class NotFoundException : ArchiveException
{
    public NotFoundException(string message)
        : base(HttpStatusCode.NotFound, message)
    {
    }
}
