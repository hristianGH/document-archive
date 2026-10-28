using System.ComponentModel;
using System.Text.Json;

namespace Shared.Models.Documents;

public class SetIndexValueRequest
{
    [Description("A string for text fields (\"\" allowed), a number for number fields, a yyyy-MM-dd string for date fields. null is rejected.")]
    public JsonElement Value { get; set; }
}
