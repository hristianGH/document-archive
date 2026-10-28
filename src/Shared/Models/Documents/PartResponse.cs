namespace Shared.Models.Documents;

public class PartResponse
{
    public Guid Id { get; set; }

    public int Position { get; set; }

    public string FileName { get; set; } = string.Empty;

    public long Size { get; set; }
}
