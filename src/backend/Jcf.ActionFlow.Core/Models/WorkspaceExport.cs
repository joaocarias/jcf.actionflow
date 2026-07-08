using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

/// <summary>
/// Root document of a Watson Assistant "action" workspace export.
/// Every field not explicitly modeled is preserved via <see cref="ExtensionData"/>
/// so the document can be re-exported without loss.
/// </summary>
public sealed class WorkspaceExport
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("skill_id")]
    public string? SkillId { get; set; }

    [JsonPropertyName("workspace_id")]
    public string? WorkspaceId { get; set; }

    [JsonPropertyName("assistant_id")]
    public string? AssistantId { get; set; }

    [JsonPropertyName("workspace")]
    public WorkspaceData Workspace { get; set; } = new();

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
