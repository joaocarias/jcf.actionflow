using System.Text.Json;

namespace Jcf.ActionFlow.Tests.Support;

/// <summary>
/// Semantic (property-order-independent) JSON comparison, used to prove round-trip
/// serialization doesn't lose or alter data.
/// </summary>
public static class JsonAssert
{
    public static void DeepEqual(string expectedJson, string actualJson)
    {
        using var expectedDoc = JsonDocument.Parse(expectedJson);
        using var actualDoc = JsonDocument.Parse(actualJson);

        var diff = FindDifference(expectedDoc.RootElement, actualDoc.RootElement, "$");
        if (diff is not null)
        {
            throw new Xunit.Sdk.XunitException($"JSON divergiu em {diff}");
        }
    }

    private static string? FindDifference(JsonElement expected, JsonElement actual, string path)
    {
        if (expected.ValueKind != actual.ValueKind)
        {
            return $"{path}: tipo esperado {expected.ValueKind}, obtido {actual.ValueKind}";
        }

        switch (expected.ValueKind)
        {
            case JsonValueKind.Object:
                var expectedProps = expected.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);
                var actualProps = actual.EnumerateObject().ToDictionary(p => p.Name, p => p.Value);

                foreach (var (key, expectedValue) in expectedProps)
                {
                    if (!actualProps.TryGetValue(key, out var actualValue))
                    {
                        return $"{path}.{key}: ausente no resultado";
                    }

                    var diff = FindDifference(expectedValue, actualValue, $"{path}.{key}");
                    if (diff is not null) return diff;
                }

                foreach (var key in actualProps.Keys)
                {
                    if (!expectedProps.ContainsKey(key))
                    {
                        return $"{path}.{key}: campo extra que não existia no original";
                    }
                }

                return null;

            case JsonValueKind.Array:
                var expectedItems = expected.EnumerateArray().ToList();
                var actualItems = actual.EnumerateArray().ToList();
                if (expectedItems.Count != actualItems.Count)
                {
                    return $"{path}: array com {expectedItems.Count} itens esperado, {actualItems.Count} obtido";
                }

                for (var i = 0; i < expectedItems.Count; i++)
                {
                    var diff = FindDifference(expectedItems[i], actualItems[i], $"{path}[{i}]");
                    if (diff is not null) return diff;
                }

                return null;

            case JsonValueKind.String:
                return expected.GetString() == actual.GetString()
                    ? null
                    : $"{path}: \"{expected.GetString()}\" != \"{actual.GetString()}\"";

            case JsonValueKind.Number:
                if (expected.GetRawText() == actual.GetRawText()) return null;
                if (decimal.TryParse(expected.GetRawText(), out var expectedNumber)
                    && decimal.TryParse(actual.GetRawText(), out var actualNumber)
                    && expectedNumber == actualNumber)
                {
                    return null;
                }

                return $"{path}: {expected.GetRawText()} != {actual.GetRawText()}";

            default:
                // True / False / Null: ValueKind equality already checked above.
                return null;
        }
    }
}
