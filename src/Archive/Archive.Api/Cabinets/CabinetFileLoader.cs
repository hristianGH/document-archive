using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using Archive.Core.Cabinets;

namespace Archive.Api.Cabinets;

public static class CabinetFileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        RespectNullableAnnotations = true,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { RequireNonNullableProperties } }
    };

    private static void RequireNonNullableProperties(JsonTypeInfo typeInfo)
    {
        foreach (var property in typeInfo.Properties)
        {
            property.IsRequired = !property.IsSetNullable;
        }
    }

    public static CabinetCatalog Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new InvalidOperationException($"Cabinets file '{path}' was not found. Set Cabinets__Path.");
        }

        try
        {
            using var stream = File.OpenRead(path);
            var file = JsonSerializer.Deserialize<CabinetsFile>(stream, JsonOptions)
                ?? throw new InvalidOperationException($"Cabinets file '{path}' is empty.");
            return new CabinetCatalog(file.Cabinets);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException($"Cabinets file '{path}' is invalid: {ex.Message}", ex);
        }
    }

    private class CabinetsFile
    {
        public IReadOnlyList<Cabinet> Cabinets { get; set; } = [];
    }
}
