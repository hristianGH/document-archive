namespace Archive.Core.Storage;

public interface IPartFileStore
{
    Stream? OpenRead(string filePath);

    Task<long> WriteAsync(string filePath, Stream content, CancellationToken cancellationToken);

    void Delete(string filePath);
}
