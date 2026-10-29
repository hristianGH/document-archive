using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace Shared.Models.Exports;

public class ExportRequest
{
    [Required, Description("The file cabinet; case is ignored.")]
    public string Cabinet { get; set; } = string.Empty;

    [Required, Description("A writable text field of the cabinet that fits the marker 'Exported on yyyy-MM-dd'; case is ignored.")]
    public string Field { get; set; } = string.Empty;

    [Required, Description("Inclusive. UTC. Format yyyy-MM-ddTHH:mm:ssZ, or yyyy-MM-dd for 00:00:00 UTC.")]
    public string From { get; set; } = string.Empty;

    [Required, Description("Inclusive. UTC. Format yyyy-MM-ddTHH:mm:ssZ, or yyyy-MM-dd for 23:59:59 UTC (the whole day).")]
    public string To { get; set; } = string.Empty;
}