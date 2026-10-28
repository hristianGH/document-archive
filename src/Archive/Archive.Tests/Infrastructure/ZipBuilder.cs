using System.IO.Compression;
using System.Text;

namespace Archive.Tests.Infrastructure;

public static class ZipBuilder
{
    // A path ending in '/' is a folder entry; any other path is a file with the given text as its content.
    public static byte[] Create(params (string Path, string Content)[] entries)
    {
        using var stream = new MemoryStream();
        using (var archive = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            foreach (var (path, content) in entries)
            {
                var entry = archive.CreateEntry(path);
                if (!path.EndsWith('/'))
                {
                    using var entryStream = entry.Open();
                    entryStream.Write(Encoding.UTF8.GetBytes(content));
                }
            }
        }

        return stream.ToArray();
    }
}