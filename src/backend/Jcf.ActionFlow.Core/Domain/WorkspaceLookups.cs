using Jcf.ActionFlow.Core.Exceptions;
using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Domain;

/// <summary>
/// Small lookups over a <see cref="WorkspaceData"/> shared by the service, graph builders,
/// copy service, and validator, so they all agree on what "which collection is this action
/// in" and "what points at this action" mean.
/// </summary>
public static class WorkspaceLookups
{
    public static ActionDefinition FindAction(WorkspaceData workspace, string actionId) =>
        workspace.Actions.FirstOrDefault(a => a.Action == actionId)
        ?? throw new ActionNotFoundException(actionId);

    public static Dictionary<string, ActionDefinition> ActionsById(WorkspaceData workspace) =>
        workspace.Actions.ToDictionary(a => a.Action, StringComparer.Ordinal);

    /// <summary>
    /// First collection each action is referenced from. Grouped defensively: if the same
    /// action were referenced from more than one collection (a data problem the validator
    /// flags on its own), this keeps the first match instead of throwing.
    /// </summary>
    public static Dictionary<string, string> CollectionIdByAction(WorkspaceData workspace) =>
        workspace.Collections
            .SelectMany(c => c.ActionReferences.Select(r => (r.Action, c.CollectionId)))
            .GroupBy(x => x.Action, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First().CollectionId, StringComparer.Ordinal);

    /// <summary>
    /// Every place (other than the action itself) that points at <paramref name="actionId"/>:
    /// other actions' next_action, and invoke_another_action in steps or step handlers.
    /// </summary>
    public static List<string> FindReferences(WorkspaceData workspace, string actionId)
    {
        var refs = new List<string>();

        foreach (var a in workspace.Actions)
        {
            if (a.Action == actionId) continue;

            if (a.NextAction == actionId)
            {
                refs.Add($"{a.Action}.next_action");
            }

            foreach (var step in a.Steps)
            {
                if (step.Resolver.InvokeAction?.Action == actionId)
                {
                    refs.Add($"{a.Action}.{step.StepId}.resolver.invoke_action");
                }

                if (step.Handlers is null) continue;
                foreach (var handler in step.Handlers)
                {
                    if (handler.Resolver?.InvokeAction?.Action == actionId)
                    {
                        refs.Add($"{a.Action}.{step.StepId}.handlers[].resolver.invoke_action");
                    }
                }
            }
        }

        return refs;
    }
}
