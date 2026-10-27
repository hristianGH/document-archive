using Archive.Core.Cabinets;

namespace Archive.Core.Handlers;

public interface ICabinetHandler
{
    IReadOnlyList<Cabinet> GetAll();

    Cabinet Get(string name);
}
