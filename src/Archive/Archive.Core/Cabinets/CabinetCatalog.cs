namespace Archive.Core.Cabinets;

public sealed class CabinetCatalog
{
    public CabinetCatalog(IReadOnlyList<Cabinet> cabinets)
    {
        All = cabinets;
    }

    public IReadOnlyList<Cabinet> All { get; }

    public Cabinet? Find(string name) => All.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase));
}
