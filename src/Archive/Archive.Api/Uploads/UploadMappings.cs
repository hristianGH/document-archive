using Archive.Core.Uploads;

namespace Archive.Api.Uploads;

public static class UploadMappings
{
    public static UploadResponse ToResponse(this UploadReport report) => new()
    {
        FolderName = report.FolderName,
        Field = report.Field,
        Message = report.Failed.Count > 0 ? $"Partial failure – {report.Failed.Count} failed" : null,
        Stored = [.. report.Stored.Select(ToResponse)],
        Skipped = [.. report.Skipped.Select(ToResponse)],
        Failed = [.. report.Failed.Select(ToResponse)]
    };

    private static StoredDocumentResponse ToResponse(this StoredDocument document) => new()
    {
        DocumentId = document.DocumentId,
        Title = document.Title,
        PartCount = document.PartCount
    };

    private static UploadEntryResponse ToResponse(this UploadReportEntry entry) => new()
    {
        Path = entry.Path,
        Reason = entry.Reason
    };
}
