using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Domain;

/// <summary>Another environment's action with the same base name has a different step count.</summary>
public sealed record EnvironmentStepCount(string Env, int StepCount);

/// <summary>
/// Actions in this workspace are conventionally titled "&lt;env&gt;/&lt;path&gt;" (e.g. "hml/finaliza",
/// "prod/triagem/finaliza") to mark which environment a copy belongs to. Two actions are "the same"
/// across environments when everything after the first "/" matches exactly - "hml/finaliza" and
/// "dev/finaliza" are the same action, but "hml/triagem/finaliza" and "prod/finaliza" are not.
/// This flags actions whose counterpart is missing in an environment that's otherwise present in
/// the workspace, or whose counterpart exists but has a different number of steps (a common sign a
/// change made in one environment wasn't propagated to another), so the UI can call attention to it.
/// System actions and titles without an env prefix (no "/") are not part of this convention and are
/// skipped entirely.
/// </summary>
public static class EnvironmentActionAnalyzer
{
    /// <summary>Action id -> environments its title's counterpart could not be found in.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FindMissingEnvironments(WorkspaceData workspace)
    {
        var scoped = ScopedActions(workspace);
        var allEnvironments = scoped.Select(x => x.Env).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);
        if (allEnvironments.Count < 2) return result;

        foreach (var group in scoped.GroupBy(x => x.BaseName, StringComparer.OrdinalIgnoreCase))
        {
            var presentEnvironments = group.Select(x => x.Env).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var missing = allEnvironments
                .Where(env => !presentEnvironments.Contains(env))
                .OrderBy(env => env, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (missing.Count == 0) continue;

            foreach (var item in group)
            {
                result[item.ActionId] = missing;
            }
        }

        return result;
    }

    /// <summary>
    /// Action id -> other environments (present for the same base name) whose step count differs
    /// from this action's own. Empty for actions whose base name isn't shared with any other
    /// environment, or whose counterparts all agree on the step count.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<EnvironmentStepCount>> FindStepCountMismatches(
        WorkspaceData workspace)
    {
        var scoped = ScopedActions(workspace);
        var result = new Dictionary<string, IReadOnlyList<EnvironmentStepCount>>(StringComparer.Ordinal);

        foreach (var group in scoped.GroupBy(x => x.BaseName, StringComparer.OrdinalIgnoreCase))
        {
            var groupList = group.ToList();
            if (groupList.Select(x => x.StepCount).Distinct().Count() < 2) continue;

            foreach (var item in groupList)
            {
                var others = groupList
                    .Where(x => !string.Equals(x.Env, item.Env, StringComparison.OrdinalIgnoreCase))
                    .Where(x => x.StepCount != item.StepCount)
                    .GroupBy(x => x.Env, StringComparer.OrdinalIgnoreCase)
                    .Select(g => new EnvironmentStepCount(g.Key, g.First().StepCount))
                    .OrderBy(x => x.Env, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                if (others.Count == 0) continue;
                result[item.ActionId] = others;
            }
        }

        return result;
    }

    private static List<(string ActionId, string Env, string BaseName, int StepCount)> ScopedActions(
        WorkspaceData workspace) =>
        workspace.Actions
            .Where(a => !SystemActions.IsSystemAction(a.Action))
            .Select(a => (Action: a, Parsed: TryParse(a.Title)))
            .Where(x => x.Parsed is not null)
            .Select(x => (x.Action.Action, x.Parsed!.Value.Env, x.Parsed!.Value.BaseName, x.Action.Steps.Count))
            .ToList();

    internal static (string Env, string BaseName)? TryParse(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var slashIndex = title.IndexOf('/');
        if (slashIndex <= 0 || slashIndex == title.Length - 1) return null;

        var env = title[..slashIndex];
        var baseName = title[(slashIndex + 1)..];

        // Guards against titles that merely contain a "/" as prose (e.g. "GOTO (hml/x)"),
        // which aren't meant to follow the env/path convention.
        if (env.Any(c => char.IsWhiteSpace(c) || c is '(' or ')')) return null;

        return (env, baseName);
    }
}
