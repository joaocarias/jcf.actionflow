using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Serialization;

/// <summary>
/// Central place for the JSON options used to read and write Watson Assistant
/// workspace exports, so every caller round-trips the same way.
/// </summary>
public static class WorkspaceJsonSerializer
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = null,
        // pt-br text (accents, "ã", "ç", ...) must come back out as-is, not \uXXXX escapes.
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = true,
    };

    public static WorkspaceExport Parse(string json) =>
        JsonSerializer.Deserialize<WorkspaceExport>(json, Options)
        ?? throw new JsonException("O JSON informado não pôde ser interpretado como um workspace export.");

    public static string Serialize(WorkspaceExport export) =>
        JsonSerializer.Serialize(export, Options);

    /// <summary>
    /// Deep-clones any of the workspace model types by round-tripping through JSON.
    /// Reliable for these types specifically because every one of them carries a
    /// <c>[JsonExtensionData]</c> catch-all, so nothing is lost in the round trip.
    /// </summary>
    public static T DeepClone<T>(T value) =>
        JsonSerializer.Deserialize<T>(JsonSerializer.Serialize(value, Options), Options)!;
}
