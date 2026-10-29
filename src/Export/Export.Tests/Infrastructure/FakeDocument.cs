using System.Text.Json;
using Shared.Models.Documents;

namespace Export.Tests.Infrastructure;

public class FakeDocument
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Cabinet { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public DateTime StoredAt { get; init; }

    public Dictionary<string, string> IndexValues { get; } = [];

    public IReadOnlyList<FakePart> Parts { get; init; } = [];

    public DocumentResponse ToResponse() => new()
    {
        Id = Id,
        Cabinet = Cabinet,
        Title = Title,
        StoredAt = StoredAt,
        ModifiedAt = StoredAt,
        IndexValues = JsonSerializer.SerializeToElement(IndexValues),
        Parts = [.. Parts.Select(p => new PartResponse { Id = p.Id, Position = p.Position, FileName = p.FileName, Size = p.Content.Length })]
    };

    public DocumentListItemResponse ToListItem() => new()
    {
        Id = Id,
        Cabinet = Cabinet,
        Title = Title,
        StoredAt = StoredAt,
        ModifiedAt = StoredAt,
        IndexValues = JsonSerializer.SerializeToElement(IndexValues),
        PartCount = Parts.Count
    };
}

public class FakePart
{
    public Guid Id { get; } = Guid.NewGuid();

    public int Position { get; init; }

    public string FileName { get; init; } = string.Empty;

    public byte[] Content { get; init; } = [];
}