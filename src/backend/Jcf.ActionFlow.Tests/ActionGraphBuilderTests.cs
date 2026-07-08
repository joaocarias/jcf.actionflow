using Jcf.ActionFlow.Core.Graph;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class ActionGraphBuilderTests
{
    private static FlowGraph BuildGraph() => ActionGraphBuilder.Build(SampleWorkspaceFixture.Load().Workspace);

    [Fact]
    public void One_node_per_action()
    {
        var graph = BuildGraph();

        Assert.Equal(9, graph.Nodes.Count);
    }

    [Fact]
    public void System_actions_get_their_own_group()
    {
        var graph = BuildGraph();

        string[] systemActions = ["welcome", "fallback", "anything_else", "run_always"];
        foreach (var id in systemActions)
        {
            Assert.Equal("system", graph.Nodes.Single(n => n.Id == id).Group);
        }
    }

    [Fact]
    public void Business_actions_are_grouped_by_collection()
    {
        var graph = BuildGraph();

        Assert.Equal("collection_4598", graph.Nodes.Single(n => n.Id == "action_49668-2").Group);
        foreach (var id in new[] { "action_49668", "action_8197", "action_25692", "action_8087" })
        {
            Assert.Equal("collection_45544", graph.Nodes.Single(n => n.Id == id).Group);
        }
    }

    [Fact]
    public void Invoke_edges_come_from_invoke_another_action_resolvers()
    {
        var graph = BuildGraph();

        var invokeEdges = graph.Edges.Where(e => e.Kind == EdgeKinds.Invoke).ToList();
        Assert.Equal(5, invokeEdges.Count);
        Assert.Contains(invokeEdges, e => e.Source == "action_25692" && e.Target == "action_8197");
        Assert.Contains(invokeEdges, e => e.Source == "action_8197" && e.Target == "action_25692");
    }

    [Fact]
    public void Action_8197_and_action_25692_invoke_each_other_in_a_loop()
    {
        var graph = BuildGraph();

        Assert.Contains(graph.Edges, e => e.Kind == EdgeKinds.Invoke && e.Source == "action_25692" && e.Target == "action_8197");
        Assert.Contains(graph.Edges, e => e.Kind == EdgeKinds.Invoke && e.Source == "action_8197" && e.Target == "action_25692");
    }

    [Fact]
    public void Ordering_edges_come_from_next_action_and_are_weak()
    {
        var graph = BuildGraph();

        var orderingEdges = graph.Edges.Where(e => e.Kind == EdgeKinds.Ordering).ToList();
        Assert.Equal(8, orderingEdges.Count);
        Assert.All(orderingEdges, e => Assert.True(e.Weak));
    }
}
