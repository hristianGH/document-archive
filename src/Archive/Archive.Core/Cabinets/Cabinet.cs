namespace Archive.Core.Cabinets;

public record Cabinet(string Name, string DisplayName, IReadOnlyList<IndexField> Fields)
{
    public IndexField? FindField(string name) => Fields.FirstOrDefault(f => f.Name == name);
}
