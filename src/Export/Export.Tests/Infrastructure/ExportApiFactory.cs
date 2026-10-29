using Export.Core.Clients;
using Export.Data.Clients;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Time.Testing;

namespace Export.Tests.Infrastructure;

// The real Export API on an in-memory TestServer; only the Archive (fake HTTP handler) and the clock are replaced.
// Both Archive timeouts run on Clock, so they fire only when a test advances it.
public class ExportApiFactory : WebApplicationFactory<Program>
{
    public static readonly DateTimeOffset Now = new(2026, 10, 28, 9, 30, 0, TimeSpan.Zero);

    public static readonly TimeSpan AttemptTimeout = TimeSpan.FromSeconds(10);

    public static readonly TimeSpan ContentTimeout = TimeSpan.FromSeconds(90);

    public FakeArchive Archive { get; } = new();

    public FakeTimeProvider Clock { get; } = new(Now);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("Archive:BaseUrl", "http://archive.test/");
        builder.UseSetting("Archive:MaxRetries", "3");
        builder.UseSetting("Archive:RetryDelay", "00:00:00");
        builder.UseSetting("Archive:AttemptTimeout", AttemptTimeout.ToString());
        builder.UseSetting("Archive:ContentTimeout", ContentTimeout.ToString());

        builder.ConfigureTestServices(services =>
        {
            services.AddHttpClient<IArchiveClient, ArchiveClient>().ConfigurePrimaryHttpMessageHandler(() => Archive);
            services.RemoveAll<TimeProvider>();
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}