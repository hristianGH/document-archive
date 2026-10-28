namespace Archive.Api.Uploads;

public class UploadResponse
{
    public string FolderName { get; set; } = string.Empty;

    public string Field { get; set; } = string.Empty;

    public string? Message { get; set; }

    public IReadOnlyList<StoredDocumentResponse> Stored { get; set; } = [];

    public IReadOnlyList<UploadEntryResponse> Skipped { get; set; } = [];

    public IReadOnlyList<UploadEntryResponse> Failed { get; set; } = [];
}
