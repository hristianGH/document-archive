using System.Net;

namespace Archive.Core.Exceptions;

public class PartContentMissingException : ArchiveException
{
    public PartContentMissingException(Guid partId)
        : base(HttpStatusCode.InternalServerError, $"Content for part {partId} is missing or corrupt.")
    {
    }
}
