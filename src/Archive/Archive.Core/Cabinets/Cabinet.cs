namespace Archive.Core.Cabinets;

public class Cabinet
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyList<IndexField> Fields { get; set; } = [];

    public IndexField? FindField(string name) => Fields.FirstOrDefault(f => string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase));
}
