namespace Archive.Core.Documents;

public class DocumentQuery
{
    public required string Cabinet { get; init; }

    public required DateTime FromUtc { get; init; }

    public required DateTime ToUtc { get; init; }

    public required int Page { get; init; }

    public required int PageSize { get; init; }
}
