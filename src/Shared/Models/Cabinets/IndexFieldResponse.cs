namespace Shared.Models.Cabinets;

public class IndexFieldResponse
{
    public string Name { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public FieldType Type { get; set; }

    public int? MaxLength { get; set; }

    public bool Writable { get; set; }
}
