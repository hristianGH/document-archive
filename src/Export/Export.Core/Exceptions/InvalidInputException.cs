using System.Net;

namespace Export.Core.Exceptions;

public class InvalidInputException : ExportException
{
    public InvalidInputException(string message)
        : base(HttpStatusCode.BadRequest, message)
    {
    }
}