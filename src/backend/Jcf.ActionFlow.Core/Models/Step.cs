using System.Text.Json;
using System.Text.Json.Serialization;

namespace Jcf.ActionFlow.Core.Models;

public sealed class Step
{
    [JsonPropertyName("step")]
    public string StepId { get; set; } = "";

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("variable")]
    public string? Variable { get; set; }

    [JsonPropertyName("condition")]
    public Condition? Condition { get; set; }

    [JsonPropertyName("context")]
    public StepContext? Context { get; set; }

    [JsonPropertyName("question")]
    public JsonElement? Question { get; set; }

    [JsonPropertyName("output")]
    public JsonElement? Output { get; set; }

    [JsonPropertyName("resolver")]
    public Resolver Resolver { get; set; } = new();

    [JsonPropertyName("next_step")]
    public string? NextStep { get; set; }

    [JsonPropertyName("handlers")]
    public List<StepHandler>? Handlers { get; set; }

    [JsonPropertyName("max_hits")]
    public int? MaxHits { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class StepContext
{
    [JsonPropertyName("variables")]
    public List<ContextVariableAssignment> Variables { get; set; } = [];

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

/// <summary>
/// One assignment in <c>step.context.variables[]</c>: sets <see cref="SkillVariable"/>
/// (a skill- or action-local variable) to <see cref="Value"/>, which is itself polymorphic
/// (<c>{"scalar": ...}</c>, <c>{"expression": "..."}</c>, <c>{"skill_variable": "..."}</c>).
/// </summary>
public sealed class ContextVariableAssignment
{
    [JsonPropertyName("skill_variable")]
    public string SkillVariable { get; set; } = "";

    [JsonPropertyName("value")]
    public JsonElement Value { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}

public sealed class StepHandler
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("handler")]
    public string? Handler { get; set; }

    [JsonPropertyName("resolver")]
    public Resolver? Resolver { get; set; }

    [JsonExtensionData]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }
}
