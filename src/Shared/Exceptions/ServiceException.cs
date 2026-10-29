using System.Net;

namespace Shared.Exceptions;

public abstract class ServiceException : Exception
{
    protected ServiceException(HttpStatusCode statusCode, string message, Exception? innerException = null)
        : base(message, innerException)
    {
        StatusCode = statusCode;
    }

    public HttpStatusCode StatusCode { get; }
}