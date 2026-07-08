using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

/// <summary>
/// Known values for <see cref="Resolver.Type"/>. Kept as plain strings (not an enum) so
/// resolver kinds Watson introduces later still round-trip losslessly.
/// </summary>
public static class ResolverTypes
{
    public const string Continue = "continue";
    public const string EndAction = "end_action";
    public const string ConnectToAgent = "connect_to_agent";
    public const string InvokeAnotherAction = "invoke_another_action";
    public const string InvokeAnotherActionAndEnd = "invoke_another_action_and_end";
    public const string Fallback = "fallback";
}

public sealed class Resolver
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "";

    [JsonPropertyName("invoke_action")]
    public InvokeAction? InvokeAction { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class InvokeAction
{
    [JsonPropertyName("action")]
    public string Action { get; set; } = "";

    [JsonPropertyName("result_variable")]
    public string? ResultVariable { get; set; }

    [JsonPropertyName("policy")]
    public string? Policy { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
