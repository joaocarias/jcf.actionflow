using System.Text.Json;
using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Graph;

/// <summary>
/// Renders a <see cref="Condition"/> into a short human-readable label, for graph edges
/// and validator messages. Comparison operators (neq, gte, ...) live in
/// <see cref="Condition.ExtensionData"/> since their key is dynamic.
/// </summary>
public static class ConditionFormatter
{
    public static string? Format(Condition? condition)
    {
        if (condition is null) return null;
        if (condition.Intent is not null) return $"intent: {condition.Intent}";
        if (condition.Expression is not null) return condition.Expression;
        if (condition.Entity is not null) return $"entity: {condition.Entity}";

        if (condition.ExtensionData is { Count: > 0 } extra)
        {
            var (op, operands) = extra.First();
            return $"{op}({FormatOperands(operands)})";
        }

        return null;
    }

    private static string FormatOperands(JsonElement operands)
    {
        if (operands.ValueKind != JsonValueKind.Array) return FormatOperand(operands);
        return string.Join(", ", operands.EnumerateArray().Select(FormatOperand));
    }

    private static string FormatOperand(JsonElement operand)
    {
        if (operand.ValueKind != JsonValueKind.Object) return operand.GetRawText();

        foreach (var prop in operand.EnumerateObject())
        {
            var value = prop.Value.ValueKind == JsonValueKind.String
                ? prop.Value.GetString()!
                : prop.Value.GetRawText();

            return prop.Name switch
            {
                "scalar" => value,
                "skill_variable" => $"${value}",
                "system_variable" => $"$sys.{value}",
                _ => value,
            };
        }

        return operand.GetRawText();
    }
}
