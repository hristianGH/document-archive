namespace Archive.Core.Documents;

public class Document
{
    public required Guid Id { get; init; }

    public required string Cabinet { get; init; }

    public required string Title { get; init; }

    public required DateTime StoredAt { get; init; }

    public required DateTime ModifiedAt { get; init; }

    public required string IndexValuesJson { get; init; }

    public required IReadOnlyList<Part> Parts { get; init; }
}
