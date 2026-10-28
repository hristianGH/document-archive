namespace Archive.Core.Uploads;

public class UploadReport
{
    public string FolderName { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public IReadOnlyList<StoredDocument> Stored { get; set; } = [];

    public IReadOnlyList<UploadReportEntry> Skipped { get; set; } = [];

    public IReadOnlyList<UploadReportEntry> Failed { get; set; } = [];
}
