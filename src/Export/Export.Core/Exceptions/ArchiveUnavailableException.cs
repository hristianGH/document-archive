using System.Net;

namespace Export.Core.Exceptions;

public class ArchiveUnavailableException : ExportException
{
    public ArchiveUnavailableException(Exception innerException)
        : base(HttpStatusCode.ServiceUnavailable, "The Archive could not be reached.", innerException)
    {
    }
}