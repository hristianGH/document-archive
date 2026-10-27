namespace Archive.Core.Documents;

public class PartContent
{
    public required string FileName { get; init; }

    public required Stream Content { get; init; }
}
