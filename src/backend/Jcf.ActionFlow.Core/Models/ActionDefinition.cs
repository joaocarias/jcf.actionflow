using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

public sealed class ActionDefinition
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("condition")]
    public Condition? Condition { get; set; }

    [JsonPropertyName("steps")]
    public List<Step> Steps { get; set; } = [];

    [JsonPropertyName("variables")]
    public List<VariableDeclaration> Variables { get; set; } = [];

    [JsonPropertyName("next_action")]
    public string? NextAction { get; set; }

    [JsonPropertyName("launch_mode")]
    public string? LaunchMode { get; set; }

    [JsonPropertyName("disambiguation_opt_out")]
    public bool? DisambiguationOptOut { get; set; }

    [JsonPropertyName("display_name")]
    public string? DisplayName { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
