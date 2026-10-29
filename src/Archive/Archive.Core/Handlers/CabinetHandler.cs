using Archive.Core.Cabinets;
using Archive.Core.Exceptions;
using Microsoft.Extensions.Logging;
using Shared.Logging;

namespace Archive.Core.Handlers;

public class CabinetHandler(CabinetCatalog catalog, ILogger<CabinetHandler> logger) : ICabinetHandler
{
    public IReadOnlyList<Cabinet> GetAll()
    {
        IReadOnlyList<Cabinet>? cabinets = null;
        logger.LogInformation("Start CabinetHandler.GetAll");
        try
        {
            cabinets = catalog.All;
            return cabinets;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error CabinetHandler.GetAll");
            throw;
        }
        finally
        {
            logger.LogInformation("End CabinetHandler.GetAll; cabinets={CabinetCount}", cabinets?.Count);
        }
    }

    public Cabinet Get(string name)
    {
        Cabinet? cabinet = null;
        logger.LogInformation("Start CabinetHandler.Get with params name={Name}", name);
        try
        {
            cabinet = catalog.Find(name) ?? throw new NotFoundException($"Cabinet '{name}' was not found.");
            return cabinet;
        }
        catch (Exception ex)
        {
            logger.LogMethodError(ex, "Error CabinetHandler.Get with params name={Name}", name);
            throw;
        }
        finally
        {
            logger.LogInformation("End CabinetHandler.Get with params name={Name}; fields={FieldCount}", name, cabinet?.Fields.Count);
        }
    }
}
