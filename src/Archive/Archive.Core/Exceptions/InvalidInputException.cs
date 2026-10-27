using System.Net;

namespace Archive.Core.Exceptions;

public class InvalidInputException : ArchiveException
{
    public InvalidInputException(string message)
        : base(HttpStatusCode.BadRequest, message)
    {
    }
}
