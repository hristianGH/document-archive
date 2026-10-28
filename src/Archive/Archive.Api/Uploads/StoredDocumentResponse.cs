namespace Archive.Api.Uploads;

public class StoredDocumentResponse
{
    public Guid DocumentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int PartCount { get; set; }
}
