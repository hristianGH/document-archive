namespace Archive.Tests.Infrastructure;

public static class TestFiles
{
    public static string CabinetsJson => Get("cabinets.json");

    public static string SampleZip => Get("sample.zip");

    public static string DamagedZip => Get("damaged.zip");

    public static string AllDamagedZip => Get("all-damaged.zip");

    public static string InvalidNameZip => Get("invalid-name.zip");

    private static string Get(string fileName) => Path.Combine(AppContext.BaseDirectory, "TestData", fileName);
}