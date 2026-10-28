namespace Archive.Core.Uploads;

public class UploadPlan
{
    public string FolderName { get; set; } = string.Empty;

    public IReadOnlyList<PlannedDocument> Documents { get; set; } = [];

    public IReadOnlyList<UploadReportEntry> Skipped { get; set; } = [];
}
