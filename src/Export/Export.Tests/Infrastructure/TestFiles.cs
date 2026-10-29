using System.Text.Json;
using Shared.Models.Cabinets;

namespace Export.Tests.Infrastructure;

public static class TestFiles
{
    public static string CabinetsJson => Get("cabinets.json");

    public static IReadOnlyList<CabinetResponse> Cabinets { get; } =
        JsonSerializer.Deserialize<CabinetsFile>(File.ReadAllText(CabinetsJson), HttpClientExtensions.JsonOptions)!.Cabinets;

    private static string Get(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);

    private class CabinetsFile
    {
        public IReadOnlyList<CabinetResponse> Cabinets { get; set; } = [];
    }
}