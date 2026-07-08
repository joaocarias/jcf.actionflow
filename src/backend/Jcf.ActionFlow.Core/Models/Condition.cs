using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

/// <summary>
/// A condition guarding an action or a step. The three common shapes
/// (<c>{"intent": "..."}</c>, <c>{"expression": "..."}</c>, <c>{"entity": "..."}</c>)
/// are typed; comparison operators (<c>neq</c>, <c>gte</c>, <c>lt</c>, <c>lte</c>, <c>gt</c>,
/// <c>eq</c>, ...) are dynamic keys and fall through to <see cref="ExtensionData"/>,
/// which keeps them fully intact for round-trip and available for label rendering.
/// </summary>
public sealed class Condition
{
    [JsonPropertyName("intent")]
    public string? Intent { get; set; }

    [JsonPropertyName("expression")]
    public string? Expression { get; set; }

    [JsonPropertyName("entity")]
    public string? Entity { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
