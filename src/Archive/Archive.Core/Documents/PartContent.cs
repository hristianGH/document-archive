namespace Archive.Core.Documents;

public class PartContent
{
    public string FileName { get; set; } = string.Empty;

    public Stream Content { get; set; } = Stream.Null;
}
