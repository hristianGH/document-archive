namespace Shared.Models.Cabinets;

public record IndexFieldDto(string Name, string Label, FieldTypeDto Type, int? MaxLength, bool Writable);
