using System.Text.Json;

namespace Shared.Models.Documents;

public class DocumentListItemResponse
{
    public required Guid Id { get; init; }

    public required string Cabinet { get; init; }

    public required string Title { get; init; }

    public required DateTime StoredAt { get; init; }

    public required DateTime ModifiedAt { get; init; }

    public required JsonElement IndexValues { get; init; }

    public required int PartCount { get; init; }
}
