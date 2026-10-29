namespace Export.Core.Exports;

public sealed class ZipNameAllocator
{
    // Ignoring case: names differing only in case unpack to the same file on Windows and macOS.
    private readonly HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

    public string AllocateFile(string name)
    {
        var dot = name.LastIndexOf('.');
        return dot > 0
            ? Allocate(name, name[..dot], name[dot..])
            : Allocate(name, name, string.Empty);
    }

    public string AllocateFolder(string name) => Allocate(name, name, string.Empty);

    private string Allocate(string name, string stem, string extension)
    {
        var candidate = name;
        for (var number = 2; !names.Add(candidate); number++)
        {
            candidate = $"{stem} ({number}){extension}";
        }

        return candidate;
    }
}