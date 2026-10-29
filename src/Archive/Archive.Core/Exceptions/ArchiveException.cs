using System.Net;
using Shared.Exceptions;

namespace Archive.Core.Exceptions;

public abstract class ArchiveException : ServiceException
{
    protected ArchiveException(HttpStatusCode statusCode, string message, Exception? innerException = null)
        : base(statusCode, message, innerException)
    {
    }
}