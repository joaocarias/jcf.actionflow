using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Graph;

/// <summary>
/// Builds the "actions" level graph: one node per action, grouped by collection
/// (system actions get their own "system" group; actions in no collection get "unassigned").
/// Edges come from invoke_another_action resolvers ("invoke") and, more loosely, from
/// next_action ("ordering", flagged weak since it's just Watson's fallback chain, not a
/// real branch).
/// </summary>
public static class ActionGraphBuilder
{
    public static FlowGraph Build(WorkspaceData workspace)
    {
        var actionIds = workspace.Actions.Select(a => a.Action).ToHashSet(StringComparer.Ordinal);
        var collectionByAction = WorkspaceLookups.CollectionIdByAction(workspace);
        var missingEnvByAction = EnvironmentActionAnalyzer.FindMissingEnvironments(workspace);
        var stepMismatchByAction = EnvironmentActionAnalyzer.FindStepCountMismatches(workspace);
        var unusedVariablesByAction = UnusedVariableAnalyzer.FindUnusedVariables(workspace);
        IReadOnlyList<string> noMissingEnvironments = [];
        IReadOnlyList<EnvironmentStepCount> noStepCountMismatches = [];
        IReadOnlyList<string> noUnusedVariables = [];

        var nodes = workspace.Actions.Select(a =>
        {
            var isSystem = SystemActions.IsSystemAction(a.Action);
            var group = isSystem ? "system" : collectionByAction.GetValueOrDefault(a.Action, "unassigned");
            var data = new Dictionary<string, object?>
            {
                ["title"] = a.Title,
                ["isSystem"] = isSystem,
                ["stepCount"] = a.Steps.Count,
                ["launchMode"] = a.LaunchMode,
                ["missingInEnvironments"] = missingEnvByAction.GetValueOrDefault(a.Action, noMissingEnvironments),
                ["stepCountMismatches"] = stepMismatchByAction.GetValueOrDefault(a.Action, noStepCountMismatches),
                ["unusedVariables"] = unusedVariablesByAction.GetValueOrDefault(a.Action, noUnusedVariables),
            };
            return new GraphNode(a.Action, a.Title ?? a.Action, group, "action", data);
        }).ToList();

        var edges = new List<GraphEdge>();
        var seenInvokeEdges = new HashSet<(string Source, string Target)>();

        foreach (var action in workspace.Actions)
        {
            foreach (var step in action.Steps)
            {
                var target = step.Resolver.InvokeAction?.Action;
                if (target is null || !actionIds.Contains(target)) continue;
                if (!seenInvokeEdges.Add((action.Action, target))) continue;

                edges.Add(new GraphEdge(
                    $"invoke:{action.Action}->{target}",
                    action.Action,
                    target,
                    EdgeKinds.Invoke));
            }

            if (action.NextAction is { } next && actionIds.Contains(next))
            {
                edges.Add(new GraphEdge(
                    $"next:{action.Action}->{next}",
                    action.Action,
                    next,
                    EdgeKinds.Ordering,
                    Weak: true));
            }
        }

        return new FlowGraph(nodes, edges);
    }
}
