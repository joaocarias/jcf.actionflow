using Jcf.ActionFlow.Core.Graph;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class StepGraphBuilderTests
{
    private static FlowGraph BuildGraph() => StepGraphBuilder.Build(SampleWorkspaceFixture.Load().Workspace);

    [Fact]
    public void One_node_per_step_namespaced_by_action()
    {
        var graph = BuildGraph();

        // 23 steps total across the 9 actions, including the two "step_798" steps that
        // exist in both action_49668 and its prod clone action_49668-2.
        Assert.Equal(23, graph.Nodes.Count);
        Assert.Contains(graph.Nodes, n => n.Id == "action_49668::step_798");
        Assert.Contains(graph.Nodes, n => n.Id == "action_49668-2::step_798");
    }

    [Fact]
    public void Sequence_edges_come_from_next_step()
    {
        var graph = BuildGraph();

        var sequenceEdges = graph.Edges.Where(e => e.Kind == EdgeKinds.Sequence).ToList();
        Assert.Equal(14, sequenceEdges.Count);
        Assert.Contains(sequenceEdges, e =>
            e.Source == "action_25692::step_174" && e.Target == "action_25692::step_801");
    }

    [Fact]
    public void Invoke_edges_cross_into_the_target_actions_first_step()
    {
        var graph = BuildGraph();

        var invokeEdges = graph.Edges.Where(e => e.Kind == EdgeKinds.Invoke).ToList();
        Assert.Equal(5, invokeEdges.Count);
        Assert.Contains(invokeEdges, e =>
            e.Source == "action_25692::step_848" && e.Target == "action_8197::step_222");
        Assert.Contains(invokeEdges, e =>
            e.Source == "action_8197::step_280" && e.Target == "action_25692::step_174");
    }

    [Fact]
    public void Conditional_steps_label_their_outgoing_edges_with_the_condition()
    {
        var graph = BuildGraph();

        // step_440 (action_8197) has both a next_step (sequence) and an invoke, guarded by
        // the same "gte" condition — both outgoing edges should carry that label.
        var fromStep440 = graph.Edges.Where(e => e.Source == "action_8197::step_440").ToList();
        Assert.Equal(2, fromStep440.Count);
        Assert.All(fromStep440, e => Assert.Contains("gte", e.Label));
    }
}
