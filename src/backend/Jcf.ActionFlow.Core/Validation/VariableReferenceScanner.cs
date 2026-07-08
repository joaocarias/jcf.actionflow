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
}
