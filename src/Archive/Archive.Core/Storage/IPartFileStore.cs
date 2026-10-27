namespace Archive.Core.Storage;

public interface IPartFileStore
{
    Stream? OpenRead(string filePath);
}
