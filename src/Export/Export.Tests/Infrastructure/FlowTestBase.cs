namespace Export.Tests.Infrastructure;

public abstract class FlowTestBase : IDisposable
{
    protected const string From = "2026-10-01";

    protected const string To = "2026-10-28";

    protected const string Marker = "Exported on 2026-10-28";

    // The zip of FakeArchive.SeedStandard exported from From to To.
    protected static readonly IReadOnlyList<ZipItem> StandardZip =
    [
        new("invoice-001.txt", "first"),
        new("Фактури Q1/януари.txt", "Януари"),
        new("Фактури Q1/февруари.txt", "Февруари"),
        new("Фактури Q1/март.txt", "Март"),
        new("empty-folder/", ""),
        new("invoice-001 (2).txt", "second"),
        new("Ünï 名前.txt", "Ünï 名前")
    ];

    protected FlowTestBase()
    {
        Factory = new ExportApiFactory();
        Client = Factory.CreateClient();
    }

    protected ExportApiFactory Factory { get; }

    protected FakeArchive Archive => Factory.Archive;

    protected HttpClient Client { get; }

    protected Task<HttpResponseMessage> ExportStandardAsync() => Client.ExportAsync("Invoices", "Batch", From, To);

    protected Task<HttpResponseMessage> StartStandardExportAsync() => Client.StartExportAsync("Invoices", "Batch", From, To);

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}