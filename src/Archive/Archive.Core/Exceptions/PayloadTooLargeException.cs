using System.Net;

namespace Archive.Core.Exceptions;

public class PayloadTooLargeException : ArchiveException
{
    public PayloadTooLargeException(string message)
        : base(HttpStatusCode.RequestEntityTooLarge, message)
    {
    }
}
