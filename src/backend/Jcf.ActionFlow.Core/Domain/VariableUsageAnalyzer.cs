using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Validation;

namespace Jcf.ActionFlow.Core.Domain;

/// <summary>One entry of workspace.variables[], with where (if anywhere) it's set and used.</summary>
public sealed record VariableUsage(
    string Variable,
    string? Title,
    string? DataType,
    bool IsSet,
    IReadOnlyList<string> SetInActions,
    bool HasDefaultValue,
    bool IsUsed,
    IReadOnlyList<string> UsedInActions);

/// <summary>
/// Reports, for every variable declared in workspace.variables[], which actions set it (via a
/// step's context.variables[].skill_variable, or the declaration's own "initial_value") and
/// which actions read it afterward. See <see cref="VariableReferenceScanner"/> for what counts
/// as a "read" vs a "write".
/// </summary>
public static class VariableUsageAnalyzer
{
    public static IReadOnlyList<VariableUsage> Analyze(WorkspaceData workspace)
    {
        var skillVariableNames = workspace.Variables.Select(v => v.Variable).ToHashSet(StringComparer.Ordinal);
        var setInActions = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        var usedInActions = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var action in workspace.Actions)
        {
            var written = new HashSet<string>(StringComparer.Ordinal);
            var read = new HashSet<string>(VariableReferenceScanner.FindReads(action.Condition?.Expression), StringComparer.Ordinal);

            foreach (var step in action.Steps)
            {
                foreach (var name in VariableReferenceScanner.FindWrites(step)) written.Add(name);
                read.UnionWith(VariableReferenceScanner.FindReads(step));
            }

            foreach (var name in written)
            {
                if (skillVariableNames.Contains(name)) Track(setInActions, name, action.Action);
            }
            foreach (var name in read)
            {
                if (skillVariableNames.Contains(name)) Track(usedInActions, name, action.Action);
            }
        }

        IReadOnlyList<string> none = [];
        return workspace.Variables
            .Select(v =>
            {
                var setActions = setInActions.GetValueOrDefault(v.Variable) ?? [];
                var usedActions = usedInActions.GetValueOrDefault(v.Variable) ?? [];
                var hasInitialValue = v.ExtensionData?.ContainsKey("initial_value") == true;

                return new VariableUsage(
                    v.Variable,
                    v.Title,
                    v.DataType,
                    IsSet: setActions.Count > 0 || hasInitialValue,
                    setActions.Count > 0 ? setActions : none,
                    HasDefaultValue: hasInitialValue,
                    IsUsed: usedActions.Count > 0,
                    usedActions.Count > 0 ? usedActions : none);
            })
            .OrderBy(v => v.Variable, StringComparer.Ordinal)
            .ToList();
    }

    private static void Track(Dictionary<string, List<string>> map, string variable, string actionId)
    {
        if (!map.TryGetValue(variable, out var actions))
        {
            actions = [];
            map[variable] = actions;
        }

        if (!actions.Contains(actionId)) actions.Add(actionId);
    }
}
