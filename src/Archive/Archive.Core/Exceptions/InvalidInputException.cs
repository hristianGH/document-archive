using System.Net;

namespace Archive.Core.Exceptions;

public class InvalidInputException : ArchiveException
{
    public InvalidInputException(string message, Exception? innerException = null)
        : base(HttpStatusCode.BadRequest, message, innerException)
    {
    }
}
