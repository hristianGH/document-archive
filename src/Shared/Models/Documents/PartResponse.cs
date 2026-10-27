namespace Shared.Models.Documents;

public class PartResponse
{
    public required Guid Id { get; init; }

    public required int Position { get; init; }

    public required string FileName { get; init; }

    public required long Size { get; init; }
}
