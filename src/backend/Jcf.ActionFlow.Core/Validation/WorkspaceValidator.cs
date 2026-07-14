using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Validation;

/// <summary>
/// Checks a workspace for dangling references, orphaned intents, duplicate collection
/// membership, and undeclared variable usage. Runs after every write endpoint; issues are
/// informational (nothing here blocks an operation on its own — see the individual write
/// endpoints for what they refuse outright, like deleting a referenced action).
/// </summary>
public static class WorkspaceValidator
{
    public static IReadOnlyList<ValidationIssue> Validate(WorkspaceData workspace)
    {
        var issues = new List<ValidationIssue>();

        var actionsById = WorkspaceLookups.ActionsById(workspace);
        var intentNames = workspace.Intents.Select(i => i.Name).ToHashSet(StringComparer.Ordinal);
        var skillVariableNames = workspace.Variables.Select(v => v.Variable).ToHashSet(StringComparer.Ordinal);
        var collectionByAction = WorkspaceLookups.CollectionIdByAction(workspace);
        var usedIntentNames = new HashSet<string>(StringComparer.Ordinal);

        foreach (var action in workspace.Actions)
        {
            var localVariableNames = action.Variables.Select(v => v.Variable).ToHashSet(StringComparer.Ordinal);
            var stepIds = action.Steps.Select(s => s.StepId).ToHashSet(StringComparer.Ordinal);

            if (action.Condition?.Intent is { } intentName)
            {
                usedIntentNames.Add(intentName);
                if (!intentNames.Contains(intentName))
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error,
                        "action.condition.intent.missing",
                        $"Action '{action.Action}' referencia o intent '{intentName}', que não existe em workspace.intents.",
                        action.Action));
                }
            }

            if (action.NextAction is { } nextAction
                && !actionsById.ContainsKey(nextAction)
                && !SystemActions.IsSystemAction(nextAction))
            {
                issues.Add(new ValidationIssue(
                    IssueSeverity.Error,
                    "action.next_action.dangling",
                    $"Action '{action.Action}' aponta next_action para '{nextAction}', que não existe.",
                    action.Action));
            }

            foreach (var step in action.Steps)
            {
                if (step.NextStep is { } nextStep && !stepIds.Contains(nextStep))
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error,
                        "step.next_step.dangling",
                        $"Step '{step.StepId}' de '{action.Action}' aponta next_step para '{nextStep}', que não existe na mesma action.",
                        action.Action,
                        step.StepId));
                }

                CheckInvoke(step.Resolver.InvokeAction, "resolver.invoke_action", action, step, actionsById, collectionByAction, issues);
                if (step.Handlers is not null)
                {
                    foreach (var handler in step.Handlers)
                    {
                        CheckInvoke(handler.Resolver?.InvokeAction, "handlers[].resolver.invoke_action", action, step, actionsById, collectionByAction, issues);
                    }
                }

                foreach (var name in VariableReferenceScanner.FindReferences(step))
                {
                    if (localVariableNames.Contains(name) || skillVariableNames.Contains(name)) continue;
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error,
                        "step.variable.undeclared",
                        $"Step '{step.StepId}' de '{action.Action}' referencia a variável '{name}', não declarada em variables[] da action nem da skill.",
                        action.Action,
                        step.StepId));
                }
            }
        }

        var nextActionTargets = workspace.Actions
            .Select(a => a.NextAction)
            .Where(n => n is not null)
            .ToHashSet(StringComparer.Ordinal)!;

        foreach (var action in workspace.Actions)
        {
            if (SystemActions.IsSystemAction(action.Action)) continue;
            if (nextActionTargets.Contains(action.Action)) continue;

            issues.Add(new ValidationIssue(
                IssueSeverity.Error,
                "action.unreachable",
                $"Action '{action.Action}' não é apontada pelo next_action de nenhuma outra action; o Watson rejeita o import por falta de alcançabilidade (\"Each action must be reachable by all previous actions via next_action\").",
                action.Action));
        }

        foreach (var intent in workspace.Intents)
        {
            if (intent.Name.StartsWith("action_", StringComparison.Ordinal) && !usedIntentNames.Contains(intent.Name))
            {
                issues.Add(new ValidationIssue(
                    IssueSeverity.Warning,
                    "intent.orphan",
                    $"Intent '{intent.Name}' não é referenciado pela condition de nenhuma action.",
                    null));
            }
        }

        var referenceCounts = workspace.Collections
            .SelectMany(c => c.ActionReferences.Select(r => r.Action))
            .GroupBy(a => a, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.Ordinal);

        foreach (var collection in workspace.Collections)
        {
            foreach (var reference in collection.ActionReferences)
            {
                if (!actionsById.ContainsKey(reference.Action))
                {
                    issues.Add(new ValidationIssue(
                        IssueSeverity.Error,
                        "collection.action_reference.dangling",
                        $"Collection '{collection.CollectionId}' referencia a action '{reference.Action}', que não existe.",
                        reference.Action));
                }
            }
        }

        foreach (var (actionId, count) in referenceCounts)
        {
            if (count > 1)
            {
                issues.Add(new ValidationIssue(
                    IssueSeverity.Error,
                    "collection.action_reference.duplicate",
                    $"Action '{actionId}' é referenciada por {count} collections diferentes.",
                    actionId));
            }
        }

        return issues;
    }

    private static void CheckInvoke(
        InvokeAction? invoke,
        string location,
        ActionDefinition action,
        Step step,
        Dictionary<string, ActionDefinition> actionsById,
        Dictionary<string, string> collectionByAction,
        List<ValidationIssue> issues)
    {
        if (invoke is null) return;

        if (!actionsById.ContainsKey(invoke.Action) && !SystemActions.IsSystemAction(invoke.Action))
        {
            issues.Add(new ValidationIssue(
                IssueSeverity.Error,
                "step.invoke.dangling",
                $"Step '{step.StepId}' de '{action.Action}' invoca '{invoke.Action}' via {location}, que não existe.",
                action.Action,
                step.StepId));
            return;
        }

        var sourceCollection = collectionByAction.GetValueOrDefault(action.Action);
        var targetCollection = collectionByAction.GetValueOrDefault(invoke.Action);
        if (sourceCollection is not null && targetCollection is not null && sourceCollection != targetCollection)
        {
            issues.Add(new ValidationIssue(
                IssueSeverity.Warning,
                "step.invoke.cross_collection",
                $"Step '{step.StepId}' de '{action.Action}' (collection '{sourceCollection}') invoca '{invoke.Action}' (collection '{targetCollection}').",
                action.Action,
                step.StepId));
        }
    }
}
