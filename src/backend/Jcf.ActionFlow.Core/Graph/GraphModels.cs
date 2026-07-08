namespace Jcf.ActionFlow.Core.Graph;

public sealed record GraphNode(
    string Id,
    string Label,
    string? Group,
    string Kind,
    IReadOnlyDictionary<string, object?> Data);

public sealed record GraphEdge(
    string Id,
    string Source,
    string Target,
    string Kind,
    string? Label = null,
    bool Weak = false);

public sealed record FlowGraph(IReadOnlyList<GraphNode> Nodes, IReadOnlyList<GraphEdge> Edges);

public static class EdgeKinds
{
    public const string Invoke = "invoke";
    public const string Ordering = "ordering";
    public const string Sequence = "sequence";
}
