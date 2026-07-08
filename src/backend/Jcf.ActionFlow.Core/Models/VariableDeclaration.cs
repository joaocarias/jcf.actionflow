using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

/// <summary>
/// A variable declaration. Used both for action-local variables
/// (<c>action.variables[]</c>) and skill-level variables (<c>workspace.variables[]</c>) —
/// both share the same shape in the Watson export.
/// </summary>
public sealed class VariableDeclaration
{
    [JsonPropertyName("variable")]
    public string Variable { get; set; } = "";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("data_type")]
    public string? DataType { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
