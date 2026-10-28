namespace Archive.Core.Uploads;

public class StoredDocument
{
    public Guid DocumentId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int PartCount { get; set; }
}
