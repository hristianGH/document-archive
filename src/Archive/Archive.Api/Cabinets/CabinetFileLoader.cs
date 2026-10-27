using System.Text.Json;
using System.Text.Json.Serialization;
using Archive.Core.Cabinets;

namespace Archive.Api.Cabinets;

public static class CabinetFileLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

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

    private record CabinetsFile(IReadOnlyList<Cabinet> Cabinets);
}
