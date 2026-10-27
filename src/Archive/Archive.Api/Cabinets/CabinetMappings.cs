using Archive.Core.Cabinets;
using Shared.Models.Cabinets;
using CoreFieldType = Archive.Core.Cabinets.FieldType;
using ResponseFieldType = Shared.Models.Cabinets.FieldType;

namespace Archive.Api.Cabinets;

public static class CabinetMappings
{
    public static CabinetResponse ToResponse(this Cabinet cabinet) => new()
    {
        Name = cabinet.Name,
        DisplayName = cabinet.DisplayName,
        Fields = [.. cabinet.Fields.Select(ToResponse)]
    };

    public static IndexFieldResponse ToResponse(this IndexField field) => new()
    {
        Name = field.Name,
        Label = field.Label,
        Type = field.Type.ToResponse(),
        MaxLength = field.MaxLength,
        Writable = field.Writable
    };

    private static ResponseFieldType ToResponse(this CoreFieldType type) => type switch
    {
        CoreFieldType.Text => ResponseFieldType.Text,
        CoreFieldType.Number => ResponseFieldType.Number,
        CoreFieldType.Date => ResponseFieldType.Date,
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
