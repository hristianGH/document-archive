namespace Archive.Data.Entities;

public class PartEntity
{
    public Guid Id { get; set; }

    public Guid DocumentId { get; set; }

    public int Position { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long Size { get; set; }

    public string FilePath { get; set; } = string.Empty;
}
