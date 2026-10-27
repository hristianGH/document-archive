namespace Archive.Core.Cabinets;

public record IndexField(string Name, string Label, FieldType Type, int? MaxLength, bool Writable);
