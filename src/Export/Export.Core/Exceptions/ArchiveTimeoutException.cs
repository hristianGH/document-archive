using System.Net;

namespace Export.Core.Exceptions;

public class ArchiveTimeoutException : ExportException
{
    public ArchiveTimeoutException(string message, Exception innerException)
        : base(HttpStatusCode.GatewayTimeout, message, innerException)
    {
    }
}
