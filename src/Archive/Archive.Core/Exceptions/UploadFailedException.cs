using System.Net;
using Archive.Core.Uploads;

namespace Archive.Core.Exceptions;

public class UploadFailedException : ArchiveException
{
    public UploadFailedException(string message, UploadReport report, Exception? innerException = null)
        : base(HttpStatusCode.InternalServerError, message, innerException)
    {
        Report = report;
    }

    public UploadReport Report { get; }
}
