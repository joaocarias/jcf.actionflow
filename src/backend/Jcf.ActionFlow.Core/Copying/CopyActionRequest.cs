using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Copying;

public static class CopyModes
{
    public const string Copy = "copy";
    public const string Move = "move";
}

public static class ReferenceStrategies
{
    public const string Keep = "keep";
    public const string Remap = "remap";
}

/// <param name="TargetCollection">Either the collection id (e.g. "collection_4598") or its title (e.g. "prod").</param>
/// <param name="ReplaceActionId">
/// When set (mode=copy only), overwrite this existing action instead of minting a new
/// suffixed id. Must already be in <paramref name="TargetCollection"/> and share the same
/// root id as the action being copied (e.g. "action_49668" and "action_49668-2").
/// </param>
public sealed record CopyActionRequest(
    string TargetCollection,
    string Mode,
    string? TitlePrefix,
    string ReferenceStrategy,
    string? ReplaceActionId = null);

public sealed record CopyActionResult(ActionDefinition Action, IReadOnlyList<string> Warnings);
