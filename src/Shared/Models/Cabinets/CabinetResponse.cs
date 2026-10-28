namespace Shared.Models.Cabinets;

public class CabinetResponse
{
    public string Name { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public IReadOnlyList<IndexFieldResponse> Fields { get; set; } = [];
}
