using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

public sealed class Intent
{
    [JsonPropertyName("intent")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("examples")]
    public List<IntentExample> Examples { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class IntentExample
{
    [JsonPropertyName("text")]
    public string Text { get; set; } = "";

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
