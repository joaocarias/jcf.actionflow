using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Graph;

/// <summary>
/// Builds the "steps" level graph across every action: one node per step, namespaced as
/// "{actionId}::{stepId}" since step ids repeat across actions (e.g. cloned actions keep
/// the same step ids as their source). Edges: next_step ("sequence"), invoke_another_action
/// crossing into the target action's first step ("invoke"), both labeled with the step's
/// own condition expression when it has one.
/// </summary>
public static class StepGraphBuilder
{
    public static FlowGraph Build(WorkspaceData workspace)
    {
        var actionsById = WorkspaceLookups.ActionsById(workspace);
        var nodes = new List<GraphNode>();
        var edges = new List<GraphEdge>();

        foreach (var action in workspace.Actions)
        {
            var stepIds = action.Steps.Select(s => s.StepId).ToHashSet(StringComparer.Ordinal);

            foreach (var step in action.Steps)
            {
                var nodeId = NodeId(action.Action, step.StepId);
                var data = new Dictionary<string, object?>
                {
                    ["actionId"] = action.Action,
                    ["resolverType"] = step.Resolver.Type,
                    ["variable"] = step.Variable,
                };
                nodes.Add(new GraphNode(nodeId, step.Title ?? step.StepId, action.Action, "step", data));

                var label = ConditionFormatter.Format(step.Condition);

                if (step.NextStep is { } nextStep && stepIds.Contains(nextStep))
                {
                    var targetId = NodeId(action.Action, nextStep);
                    edges.Add(new GraphEdge(
                        $"seq:{nodeId}->{targetId}",
                        nodeId,
                        targetId,
                        EdgeKinds.Sequence,
                        label));
                }

                var invokeTarget = step.Resolver.InvokeAction?.Action;
                if (invokeTarget is not null && actionsById.TryGetValue(invokeTarget, out var targetAction))
                {
                    var entryStep = targetAction.Steps.FirstOrDefault();
                    if (entryStep is not null)
                    {
                        var targetId = NodeId(invokeTarget, entryStep.StepId);
                        edges.Add(new GraphEdge(
                            $"invoke:{nodeId}->{targetId}",
                            nodeId,
                            targetId,
                            EdgeKinds.Invoke,
                            label));
                    }
                }
            }
        }

        return new FlowGraph(nodes, edges);
    }

    private static string NodeId(string actionId, string stepId) => $"{actionId}::{stepId}";
}
