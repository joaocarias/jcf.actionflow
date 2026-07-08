using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

public sealed class Collection
{
    [JsonPropertyName("collection")]
    public string CollectionId { get; set; } = "";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("action_references")]
    public List<ActionReference> ActionReferences { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class ActionReference
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
