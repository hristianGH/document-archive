namespace Archive.Core.Cabinets;

public class IndexField
{
    public required string Name { get; init; }

    public required string Label { get; init; }

    public required FieldType Type { get; init; }

    public int? MaxLength { get; init; }

    public required bool Writable { get; init; }
}
