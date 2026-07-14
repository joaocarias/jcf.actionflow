using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Validation;

namespace Jcf.ActionFlow.Core.Domain;

/// <summary>
/// Flags variables that get assigned somewhere but are never read afterward - dead stores.
/// Action-local variables (declared in action.Variables[], set via a step's own "variable" or
/// a resolver's "result_variable") are checked within their own action, since nothing outside
/// it can reference them. Skill variables (declared in workspace.Variables[], set via a step's
/// context.variables[].skill_variable) are checked across the whole workspace, since any
/// action can read what another one wrote; the flag is attached to whichever action(s) wrote
/// it. See <see cref="VariableReferenceScanner"/> for what counts as a "read" vs a "write".
/// </summary>
public static class UnusedVariableAnalyzer
{
    /// <summary>Action id -> variable names it assigns but never reads (nor anyone else, for skill variables).</summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> FindUnusedVariables(WorkspaceData workspace)
    {
        var unusedSkillVariables = FindUnusedSkillVariables(workspace);
        var result = new Dictionary<string, IReadOnlyList<string>>(StringComparer.Ordinal);

        foreach (var action in workspace.Actions)
        {
            var written = new HashSet<string>(StringComparer.Ordinal);
            var read = new HashSet<string>(VariableReferenceScanner.FindReads(action.Condition?.Expression), StringComparer.Ordinal);
            var skillVariablesWrittenHere = new HashSet<string>(StringComparer.Ordinal);

            foreach (var step in action.Steps)
            {
                foreach (var name in VariableReferenceScanner.FindWrites(step))
                {
                    written.Add(name);
                }
                read.UnionWith(VariableReferenceScanner.FindReads(step));

                if (step.Context is not null)
                {
                    skillVariablesWrittenHere.UnionWith(
                        step.Context.Variables.Select(v => v.SkillVariable).Where(unusedSkillVariables.Contains));
                }
            }

            var unusedLocal = action.Variables
                .Select(v => v.Variable)
                .Where(name => written.Contains(name) && !read.Contains(name));

            var unused = unusedLocal
                .Concat(skillVariablesWrittenHere)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(name => name, StringComparer.Ordinal)
                .ToList();

            if (unused.Count > 0)
            {
                result[action.Action] = unused;
            }
        }

        return result;
    }

    private static IReadOnlySet<string> FindUnusedSkillVariables(WorkspaceData workspace)
    {
        var written = new HashSet<string>(StringComparer.Ordinal);
        var read = new HashSet<string>(StringComparer.Ordinal);

        foreach (var action in workspace.Actions)
        {
            read.UnionWith(VariableReferenceScanner.FindReads(action.Condition?.Expression));

            foreach (var step in action.Steps)
            {
                if (step.Context is not null)
                {
                    written.UnionWith(step.Context.Variables.Select(v => v.SkillVariable));
                }
                read.UnionWith(VariableReferenceScanner.FindReads(step));
            }
        }

        return workspace.Variables
            .Select(v => v.Variable)
            .Where(name => written.Contains(name) && !read.Contains(name))
            .ToHashSet(StringComparer.Ordinal);
    }
}
