namespace Shared.Models.Cabinets;

public record CabinetDto(string Name, string DisplayName, IReadOnlyList<IndexFieldDto> Fields);
