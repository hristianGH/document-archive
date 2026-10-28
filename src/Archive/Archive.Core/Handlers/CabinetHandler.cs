using Archive.Core.Cabinets;
using Archive.Core.Exceptions;

namespace Archive.Core.Handlers;

public class CabinetHandler(CabinetCatalog catalog) : ICabinetHandler
{
    public IReadOnlyList<Cabinet> GetAll() => catalog.All;

    public Cabinet Get(string name) => catalog.Find(name) ?? throw new NotFoundException($"Cabinet '{name}' was not found.");
}
