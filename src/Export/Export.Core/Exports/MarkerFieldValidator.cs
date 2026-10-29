using Export.Core.Cabinets;
using Export.Core.Exceptions;

namespace Export.Core.Exports;

public static class MarkerFieldValidator
{
    public static IndexField GetMarkerField(Cabinet cabinet, string fieldName, string marker)
    {
        var field = cabinet.FindField(fieldName)
            ?? throw new InvalidInputException($"Field '{fieldName}' does not exist in cabinet '{cabinet.Name}'.");

        if (!field.Writable)
        {
            throw new InvalidInputException($"Field '{field.Name}' is not writable.");
        }

        if (field.Type != FieldType.Text)
        {
            throw new InvalidInputException($"Field '{field.Name}' is not a text field.");
        }

        if (field.MaxLength is { } maxLength && maxLength < marker.Length)
        {
            throw new InvalidInputException($"The export marker '{marker}' is {marker.Length} characters long, but '{field.Label}' ({field.Name}) holds at most {maxLength}. Choose a text field that holds at least {marker.Length} characters.");
        }

        return field;
    }
}