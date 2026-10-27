namespace Shared.Models.Cabinets;

public class CabinetResponse
{
    public required string Name { get; init; }

    public required string DisplayName { get; init; }

    public required IReadOnlyList<IndexFieldResponse> Fields { get; init; }
}
