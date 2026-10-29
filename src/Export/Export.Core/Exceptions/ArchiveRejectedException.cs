using System.Net;

namespace Export.Core.Exceptions;

public class ArchiveRejectedException : ExportException
{
    public ArchiveRejectedException(HttpStatusCode statusCode, string message)
        : base(statusCode, message)
    {
    }
}