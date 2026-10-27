using Archive.Core.Cabinets;
using Shared.Models.Cabinets;

namespace Archive.Api.Mappers;

public static class CabinetMapper
{
    public static CabinetDto ToDto(this Cabinet cabinet) =>
        new(cabinet.Name, cabinet.DisplayName, cabinet.Fields.Select(ToDto).ToList());

    public static IndexFieldDto ToDto(this IndexField field) =>
        new(field.Name, field.Label, field.Type.ToDto(), field.MaxLength, field.Writable);

    public static FieldTypeDto ToDto(this FieldType type) => type switch
    {
        FieldType.Text => FieldTypeDto.Text,
        FieldType.Number => FieldTypeDto.Number,
        FieldType.Date => FieldTypeDto.Date,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
