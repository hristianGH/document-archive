using System.Net;

namespace Archive.Core.Exceptions;

public abstract class ArchiveException : Exception
{
    protected ArchiveException(HttpStatusCode statusCode, string message)
        : base(message)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}
