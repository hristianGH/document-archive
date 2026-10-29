using System.Net;

namespace Export.Core.Exceptions;

public class ArchiveFailedException : ExportException
{
    public ArchiveFailedException(HttpStatusCode archiveStatusCode)
        : base(HttpStatusCode.BadGateway, $"The Archive failed with status {(int)archiveStatusCode}.")
    {
    }
}