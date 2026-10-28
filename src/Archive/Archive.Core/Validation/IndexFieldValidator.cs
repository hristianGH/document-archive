using System.Globalization;
using System.Text.Json;
using Archive.Core.Cabinets;
using Archive.Core.Exceptions;

namespace Archive.Core.Validation;

public static class IndexFieldValidator
{
    private const string DateFormat = "yyyy-MM-dd";

    public static IndexField GetWritableField(Cabinet cabinet, string fieldName)
    {
        var field = cabinet.FindField(fieldName)
            ?? throw new InvalidInputException($"Field '{fieldName}' does not exist in cabinet '{cabinet.Name}'.");

        return field.Writable
            ? field
            : throw new InvalidInputException($"Field '{field.Name}' is not writable.");
    }

    public static void EnsureText(IndexField field)
    {
        if (field.Type != FieldType.Text)
        {
            throw new InvalidInputException($"Field '{field.Name}' is not a text field.");
        }
    }

    public static void ValidateText(IndexField field, string value)
    {
        if (value.Length > field.MaxLength)
        {
            throw new InvalidInputException($"Value '{value}' is longer than the {field.MaxLength} characters allowed for field '{field.Name}'.");
        }
    }

    public static object ParseValue(IndexField field, JsonElement value)
    {
        if (value.ValueKind is JsonValueKind.Null or JsonValueKind.Undefined)
        {
            throw new InvalidInputException($"A value is required for field '{field.Name}'.");
        }

        switch (field.Type)
        {
            case FieldType.Text when value.ValueKind == JsonValueKind.String:
                var text = value.GetString()!;
                ValidateText(field, text);
                return text;
            case FieldType.Number when value.ValueKind == JsonValueKind.Number:
                return value.TryGetDecimal(out var number)
                    ? number
                    : throw new InvalidInputException($"The value for field '{field.Name}' is out of range.");
            case FieldType.Date when value.ValueKind == JsonValueKind.String
                && DateOnly.TryParseExact(value.GetString(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var date):
                return date.ToString(DateFormat, CultureInfo.InvariantCulture);
            default:
                throw new InvalidInputException($"Field '{field.Name}' {ExpectedValue(field.Type)}.");
        }
    }

    private static string ExpectedValue(FieldType type) => type switch
    {
        FieldType.Text => "needs a string",
        FieldType.Number => "needs a number",
        FieldType.Date => $"needs a real date in the format {DateFormat}",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
    };
}
