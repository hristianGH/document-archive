namespace Archive.Core.Documents;

public class Part
{
    public Guid Id { get; set; }

    public int Position { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long Size { get; set; }

    public string FilePath { get; set; } = string.Empty;
}
