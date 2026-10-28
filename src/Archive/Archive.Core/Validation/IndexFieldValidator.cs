using Archive.Core.Cabinets;
using Archive.Core.Exceptions;

namespace Archive.Core.Validation;

public static class IndexFieldValidator
{
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
}
