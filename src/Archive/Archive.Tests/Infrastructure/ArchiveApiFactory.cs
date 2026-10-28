using Archive.Api.Startup;
using Archive.Core.Repositories;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Archive.Tests.Infrastructure;

// The real Archive API on an in-memory TestServer; only the database (fake repository) and the clock are replaced.
public class ArchiveApiFactory(params (string Key, string Value)[] settings) : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset StartTime = new(2026, 10, 1, 9, 30, 0, TimeSpan.Zero);

    public FakeDocumentRepository Repository { get; } = new();

    public FakeTimeProvider Clock { get; } = new(StartTime);

    public string FilesPath { get; } = Path.Combine(Path.GetTempPath(), "archive-tests", Guid.NewGuid().ToString("N"));

    public int StoredFileCount => Directory.Exists(FilesPath) ? Directory.EnumerateFiles(FilesPath, "*", SearchOption.AllDirectories).Count() : 0;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("ConnectionStrings:Archive", "Host=unused;Database=unused");
        builder.UseSetting("Cabinets:Path", TestFiles.CabinetsJson);
        builder.UseSetting("Storage:FilesPath", FilesPath);
        builder.UseSetting("Paging:MaxPageSize", "5");
        builder.UseSetting("Upload:MaxZipBytes", "1048576");
        builder.UseSetting("Upload:MaxUncompressedBytes", "5242880");
        builder.UseSetting("Upload:MaxEntries", "1000");
        foreach (var (key, value) in settings)
        {
            builder.UseSetting(key, value);
        }

        builder.ConfigureTestServices(services =>
        {
            services.Remove(services.Single(s => s.ImplementationType == typeof(DatabaseMigrator)));
            services.RemoveAll<IDocumentRepository>();
            services.AddSingleton<IDocumentRepository>(Repository);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && Directory.Exists(FilesPath))
        {
            Directory.Delete(FilesPath, recursive: true);
        }
    }
}