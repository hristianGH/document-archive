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
            throw new InvalidInputException($"Field '{field.Name}' holds at most {maxLength} characters; the marker '{marker}' needs {marker.Length}.");
        }

        return field;
    }
}