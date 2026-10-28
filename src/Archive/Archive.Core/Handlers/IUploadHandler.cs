using Archive.Core.Uploads;

namespace Archive.Core.Handlers;

public interface IUploadHandler
{
    Task<UploadReport> UploadAsync(string cabinet, string field, string zipFileName, Stream zip, CancellationToken cancellationToken);
}
