using System.Net;

namespace Archive.Core.Exceptions;

public abstract class ArchiveException : Exception
{
    protected ArchiveException(HttpStatusCode statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
