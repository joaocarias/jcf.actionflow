using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

/// <summary>
/// The "workspace" object inside a Watson Assistant action export.
/// Fields we don't manipulate (entities, metadata, data_types, counterexamples,
/// system_settings, learning_opt_out, ...) are preserved via <see cref="ExtensionData"/>.
/// </summary>
public sealed class WorkspaceData
{
    [JsonPropertyName("actions")]
    public List<ActionDefinition> Actions { get; set; } = [];

    [JsonPropertyName("intents")]
    public List<Intent> Intents { get; set; } = [];

    [JsonPropertyName("variables")]
    public List<VariableDeclaration> Variables { get; set; } = [];

    [JsonPropertyName("collections")]
    public List<Collection> Collections { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
