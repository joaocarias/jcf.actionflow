using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Domain;

/// <summary>
/// Flags global variables (declared in workspace.variables[]) that get assigned somewhere but
/// are never read anywhere afterward - dead stores. Action- and step-local variables (Watson
/// auto-declares one per step in action.variables[]) are intentionally not analyzed: almost
/// none of them are ever referenced by design, so flagging them would be noise rather than a
/// real problem - only workspace-level state is worth a "you set this and nobody reads it"
/// warning. The flag is attached to whichever action(s) actually wrote the variable; a variable
/// set only via its own declaration's "initial_value" (no step writes it) has nothing to
/// attach to and won't appear here, but still shows up as unused in
/// <see cref="VariableUsageAnalyzer"/>, which this is built on top of.
/// </summary>
public static class UnusedVariableAnalyzer
{
    /// <summary>Action id -> global variable names it sets but that are never used anywhere.</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FindUnusedVariables(WorkspaceData workspace)
    {
        var result = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var usage in VariableUsageAnalyzer.Analyze(workspace))
        {
            if (usage.IsUsed) continue;

            foreach (var actionId in usage.SetInActions)
            {
                if (!result.TryGetValue(actionId, out var names))
                {
                    names = [];
                    result[actionId] = names;
                }
                names.Add(usage.Variable);
            }
        }

        return result.ToDictionary(
            kv => kv.Key,
            IReadOnlyList<string> (kv) => kv.Value.OrderBy(n => n, StringComparer.Ordinal).ToList(),
            StringComparer.Ordinal);
    }
}
