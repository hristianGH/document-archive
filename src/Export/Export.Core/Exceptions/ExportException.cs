using System.Net;
using Shared.Exceptions;

namespace Export.Core.Exceptions;

public abstract class ExportException : ServiceException
{
    protected ExportException(HttpStatusCode statusCode, string message, Exception? innerException = null)
        : base(statusCode, message, innerException)
    {
    }
}