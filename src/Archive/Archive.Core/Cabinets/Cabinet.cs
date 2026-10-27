namespace Archive.Core.Cabinets;

public class Cabinet
{
    public required string Name { get; init; }

    public required string DisplayName { get; init; }

    public required IReadOnlyList<IndexField> Fields { get; init; }

    public IndexField? FindField(string name) => Fields.FirstOrDefault(f => f.Name == name);
}
