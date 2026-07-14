using System.Text.Json;
using System.Text.RegularExpressions;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Serialization;

namespace Jcf.ActionFlow.Core.Validation;

/// <summary>
/// Finds every variable name a step refers to, wherever it shows up in the step's JSON:
/// the step's own "variable" declaration, a resolver's "result_variable", any
/// "skill_variable" key (assignment target or operand reference), and "${name}"
/// interpolations inside expression strings (conditions, context value expressions).
/// "system_variable" keys (Watson built-ins like fallback_reason) are intentionally not
/// matched — they're never declared in variables[] and shouldn't be flagged.
/// </summary>
public static class VariableReferenceScanner
{
    private static readonly Regex VariableKey = new("\"variable\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex ResultVariableKey = new("\"result_variable\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex SkillVariableKey = new("\"skill_variable\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.Compiled);
    private static readonly Regex Interpolation = new(@"\$\{([A-Za-z0-9_]+)\}", RegexOptions.Compiled);

    public static IReadOnlySet<string> FindReferences(Step step)
    {
        var json = JsonSerializer.Serialize(step, WorkspaceJsonSerializer.Options);
        var names = new HashSet<string>(StringComparer.Ordinal);

        void Collect(Regex pattern)
        {
            foreach (Match match in pattern.Matches(json))
            {
                names.Add(match.Groups[1].Value);
            }
        }

        Collect(VariableKey);
        Collect(ResultVariableKey);
        Collect(SkillVariableKey);
        Collect(Interpolation);

        return names;
    }

    /// <summary>
    /// Variable names this step assigns: its own "variable", any resolver's "result_variable"
    /// (including handlers'), and each "skill_variable" target in step.context.variables[].
    /// </summary>
    public static IReadOnlyCollection<string> FindWrites(Step step)
    {
        var writes = new List<string>();

        if (step.Variable is { } variable) writes.Add(variable);
        if (step.Resolver.InvokeAction?.ResultVariable is { } resultVariable) writes.Add(resultVariable);

        if (step.Handlers is not null)
        {
            foreach (var handler in step.Handlers)
            {
                if (handler.Resolver?.InvokeAction?.ResultVariable is { } handlerResult) writes.Add(handlerResult);
            }
        }

        if (step.Context is not null)
        {
            writes.AddRange(step.Context.Variables.Select(v => v.SkillVariable));
        }

        return writes;
    }

    /// <summary>
    /// Variable names this step reads: every "${name}" interpolation, plus any "skill_variable"
    /// that shows up as an operand (a condition comparison, or another assignment's value)
    /// rather than purely as an assignment's own target - counted by taking every
    /// "skill_variable" occurrence in the step and discarding one per assignment target, so a
    /// name used both to assign and, elsewhere in the same step, as an operand still counts as
    /// read.
    /// </summary>
    public static IReadOnlySet<string> FindReads(Step step)
    {
        var json = JsonSerializer.Serialize(step, WorkspaceJsonSerializer.Options);
        var reads = new HashSet<string>(FindReads(json), StringComparer.Ordinal);

        var writeTargetCounts = (step.Context?.Variables ?? [])
            .Select(v => v.SkillVariable)
            .GroupBy(name => name, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var group in SkillVariableKey.Matches(json).Select(m => m.Groups[1].Value).GroupBy(n => n, StringComparer.Ordinal))
        {
            if (group.Count() > writeTargetCounts.GetValueOrDefault(group.Key))
            {
                reads.Add(group.Key);
            }
        }

        return reads;
    }

    /// <summary>Every "${name}" interpolation found in <paramref name="text"/>.</summary>
    public static IReadOnlySet<string> FindReads(string? text)
    {
        var reads = new HashSet<string>(StringComparer.Ordinal);
        if (string.IsNullOrEmpty(text)) return reads;

        foreach (Match match in Interpolation.Matches(text))
        {
            reads.Add(match.Groups[1].Value);
        }

        return reads;
    }
}
