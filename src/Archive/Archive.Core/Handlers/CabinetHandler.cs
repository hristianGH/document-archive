using Archive.Core.Cabinets;
using Archive.Core.Exceptions;

namespace Archive.Core.Handlers;

public class CabinetHandler : ICabinetHandler
{
    private readonly CabinetCatalog _catalog;

    public CabinetHandler(CabinetCatalog catalog)
    {
        _catalog = catalog;
    }

    public IReadOnlyList<Cabinet> GetAll() => _catalog.All;

    public Cabinet Get(string name) => _catalog.Find(name) ?? throw new NotFoundException($"Cabinet '{name}' was not found.");
}
