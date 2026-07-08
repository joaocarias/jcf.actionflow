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
public sealed record CopyActionRequest(
    string TargetCollection,
    string Mode,
    string? TitlePrefix,
    string ReferenceStrategy);

public sealed record CopyActionResult(ActionDefinition Action, IReadOnlyList<string> Warnings);
