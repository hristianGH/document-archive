namespace Archive.Tests.Infrastructure;

public abstract class FlowTestBase : IDisposable
{
    protected FlowTestBase()
    {
        Factory = new ArchiveApiFactory();
        Client = Factory.CreateClient();
    }

    protected ArchiveApiFactory Factory { get; }

    protected HttpClient Client { get; }

    public void Dispose()
    {
        Client.Dispose();
        Factory.Dispose();
        GC.SuppressFinalize(this);
    }
}