using System.Text.RegularExpressions;
using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Exceptions;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Serialization;

namespace Jcf.ActionFlow.Core.Copying;

/// <summary>
/// Copies or moves a business action into another collection, mirroring how Watson itself
/// behaves when you duplicate an action (see the class-level algorithm notes on each private
/// method below). System actions can never be copied or moved.
/// </summary>
public sealed class ActionCopyService
{
    private static readonly Regex SuffixPattern = new(@"-\d+$", RegexOptions.Compiled);

    public CopyActionResult Execute(WorkspaceData workspace, string actionId, CopyActionRequest request)
    {
        if (SystemActions.IsSystemAction(actionId))
        {
            throw new SystemActionProtectedException(actionId);
        }

        var action = WorkspaceLookups.FindAction(workspace, actionId);

        var target = workspace.Collections.FirstOrDefault(c =>
            c.CollectionId == request.TargetCollection || c.Title == request.TargetCollection);
        if (target is null)
        {
            throw new InvalidWorkspaceException($"Collection destino '{request.TargetCollection}' não encontrada.");
        }

        return request.Mode switch
        {
            CopyModes.Move => Move(workspace, action, target),
            CopyModes.Copy => Copy(workspace, action, target, request),
            _ => throw new InvalidWorkspaceException($"mode inválido: '{request.Mode}' (use 'copy' ou 'move')."),
        };
    }

    /// <summary>
    /// Move only relocates the action_reference: no clone, no rename, the action itself
    /// is untouched — it's a pointer, not a copy.
    /// </summary>
    private static CopyActionResult Move(WorkspaceData workspace, ActionDefinition action, Collection target)
    {
        foreach (var collection in workspace.Collections)
        {
            collection.ActionReferences.RemoveAll(r => r.Action == action.Action);
        }

        if (target.ActionReferences.All(r => r.Action != action.Action))
        {
            target.ActionReferences.Add(new ActionReference { Action = action.Action });
        }

        return new CopyActionResult(action, []);
    }

    /// <summary>
    /// Copy: clone the action under a fresh suffixed id (or overwrite an existing same-root
    /// clone when <see cref="CopyActionRequest.ReplaceActionId"/> is set), clone its intent
    /// the same way, register the clone in the target collection, then resolve every outbound
    /// reference (next_action + each step's invoke_another_action) per
    /// <paramref name="request"/>'s ReferenceStrategy, splice the clone into the next_action
    /// chain (see <see cref="SpliceIntoNextActionChain"/> — required for Watson to accept the
    /// import), and finally apply the title prefix swap.
    /// </summary>
    private static CopyActionResult Copy(
        WorkspaceData workspace,
        ActionDefinition source,
        Collection target,
        CopyActionRequest request)
    {
        if (request.ReferenceStrategy is not (ReferenceStrategies.Keep or ReferenceStrategies.Remap))
        {
            throw new InvalidWorkspaceException(
                $"referenceStrategy inválido: '{request.ReferenceStrategy}' (use 'keep' ou 'remap').");
        }

        var warnings = new List<string>();
        var replaced = ResolveReplaceTarget(workspace, source, target, request.ReplaceActionId);

        string newActionId;
        if (replaced is not null)
        {
            newActionId = replaced.Action;
        }
        else
        {
            var existingActionIds = workspace.Actions.Select(a => a.Action).ToHashSet(StringComparer.Ordinal);
            newActionId = NextSuffixedId(source.Action, existingActionIds);
        }

        var clone = WorkspaceJsonSerializer.DeepClone(source);
        clone.Action = newActionId;

        if (replaced is not null)
        {
            workspace.Actions.Remove(replaced);
            if (replaced.Condition?.Intent is { } oldIntentName)
            {
                workspace.Intents.RemoveAll(i => i.Name == oldIntentName);
            }

            warnings.Add($"Action '{newActionId}' substituída pelo conteúdo atual de '{source.Action}'.");
        }

        CloneIntent(workspace, clone);

        if (target.ActionReferences.All(r => r.Action != newActionId))
        {
            target.ActionReferences.Add(new ActionReference { Action = newActionId });
        }

        var actionsById = WorkspaceLookups.ActionsById(workspace);
        var collectionByAction = WorkspaceLookups.CollectionIdByAction(workspace);

        string ResolveReference(string originalTarget, string edgeDescription) => request.ReferenceStrategy switch
        {
            ReferenceStrategies.Keep => ResolveKeep(originalTarget, edgeDescription, target, collectionByAction, warnings),
            _ => ResolveRemap(originalTarget, edgeDescription, target, actionsById, warnings),
        };

        RemapOutboundReferences(clone, ResolveReference);
        SpliceIntoNextActionChain(workspace, clone, newActionId, warnings);

        if (request.TitlePrefix is { Length: > 0 } prefix)
        {
            clone.Title = ApplyTitlePrefix(clone.Title, prefix);
        }

        workspace.Actions.Add(clone);

        return new CopyActionResult(clone, warnings);
    }

    /// <summary>
    /// Validates and resolves the action a "copy" should overwrite in place, or returns null
    /// when the request isn't asking for a replace.
    /// </summary>
    private static ActionDefinition? ResolveReplaceTarget(
        WorkspaceData workspace,
        ActionDefinition source,
        Collection target,
        string? replaceActionId)
    {
        if (replaceActionId is null) return null;

        if (SystemActions.IsSystemAction(replaceActionId))
        {
            throw new SystemActionProtectedException(replaceActionId);
        }

        var replaced = WorkspaceLookups.FindAction(workspace, replaceActionId);

        if (target.ActionReferences.All(r => r.Action != replaceActionId))
        {
            throw new InvalidWorkspaceException(
                $"'{replaceActionId}' não está na collection destino '{target.CollectionId}'.");
        }

        if (RootActionId(replaced.Action) != RootActionId(source.Action))
        {
            throw new InvalidWorkspaceException(
                $"'{replaceActionId}' não é uma cópia de '{source.Action}' (raízes de id diferentes).");
        }

        return replaced;
    }

    /// <summary>"action_49668-2" -> "action_49668". Two ids share a root when one is a clone of the other.</summary>
    public static string RootActionId(string actionId) => SuffixPattern.Replace(actionId, "");

    private static void CloneIntent(WorkspaceData workspace, ActionDefinition clone)
    {
        if (clone.Condition?.Intent is not { } originalIntentName) return;

        var existingIntentNames = workspace.Intents.Select(i => i.Name).ToHashSet(StringComparer.Ordinal);
        var newIntentName = NextSuffixedId(originalIntentName, existingIntentNames);

        var originalIntent = workspace.Intents.FirstOrDefault(i => i.Name == originalIntentName);
        if (originalIntent is not null)
        {
            var clonedIntent = WorkspaceJsonSerializer.DeepClone(originalIntent);
            clonedIntent.Name = newIntentName;
            workspace.Intents.Add(clonedIntent);
        }

        clone.Condition.Intent = newIntentName;
    }

    private static void RemapOutboundReferences(ActionDefinition clone, Func<string, string, string> resolve)
    {
        if (clone.NextAction is { } nextAction)
        {
            clone.NextAction = resolve(nextAction, $"{clone.Action}.next_action");
        }

        foreach (var step in clone.Steps)
        {
            if (step.Resolver.InvokeAction is { } invoke)
            {
                invoke.Action = resolve(invoke.Action, $"{clone.Action}.{step.StepId}.resolver.invoke_action");
            }

            if (step.Handlers is null) continue;
            foreach (var handler in step.Handlers)
            {
                if (handler.Resolver?.InvokeAction is { } handlerInvoke)
                {
                    handlerInvoke.Action = resolve(
                        handlerInvoke.Action,
                        $"{clone.Action}.{step.StepId}.handlers[].resolver.invoke_action");
                }
            }
        }
    }

    /// <summary>
    /// Watson rejects an import unless every action is reachable via next_action from some
    /// other action ("Each action must be reachable by all previous actions via next_action").
    /// A freshly appended clone has nothing pointing at it, so we splice it in front of
    /// whichever action currently flows into the same target the clone itself flows into —
    /// that predecessor now runs through the clone first, and the clone still lands on the
    /// original target afterward, so nothing downstream loses reachability.
    /// </summary>
    private static void SpliceIntoNextActionChain(
        WorkspaceData workspace,
        ActionDefinition clone,
        string newActionId,
        List<string> warnings)
    {
        if (clone.NextAction is not { } target) return;

        var predecessor = workspace.Actions.FirstOrDefault(a => a.NextAction == target);
        if (predecessor is null) return;

        predecessor.NextAction = newActionId;
        warnings.Add(
            $"'{predecessor.Action}.next_action' redirecionado para '{newActionId}' (antes '{target}') " +
            "para manter a cópia alcançável via next_action, como o Watson exige.");
    }

    /// <summary>keep: leave the reference alone, but flag it if it now crosses out of the target collection.</summary>
    private static string ResolveKeep(
        string originalTarget,
        string edgeDescription,
        Collection target,
        Dictionary<string, string> collectionByAction,
        List<string> warnings)
    {
        var targetsCollectionId = collectionByAction.GetValueOrDefault(originalTarget);
        if (targetsCollectionId is not null && targetsCollectionId != target.CollectionId)
        {
            warnings.Add(
                $"{edgeDescription}: referência para '{originalTarget}' (collection '{targetsCollectionId}') " +
                $"mantida fora da collection destino '{target.CollectionId}'.");
        }

        return originalTarget;
    }

    /// <summary>
    /// remap: swap the referenced action's environment prefix for the target collection's
    /// title, and look for a same-titled action already in the target collection. Falls
    /// back to keep-and-warn when nothing matches.
    /// </summary>
    private static string ResolveRemap(
        string originalTarget,
        string edgeDescription,
        Collection target,
        Dictionary<string, ActionDefinition> actionsById,
        List<string> warnings)
    {
        if (!actionsById.TryGetValue(originalTarget, out var originalTargetAction)
            || originalTargetAction.Title is not { } originalTitle)
        {
            warnings.Add($"{edgeDescription}: não foi possível remapear '{originalTarget}' (action ou title ausente); referência mantida.");
            return originalTarget;
        }

        var candidateTitle = target.Title is not null ? ApplyTitlePrefix(originalTitle, $"{target.Title}/") : null;

        var remapped = candidateTitle is not null
            ? target.ActionReferences
                .Select(r => actionsById.GetValueOrDefault(r.Action))
                .FirstOrDefault(a => a?.Title == candidateTitle)
            : null;

        if (remapped is not null) return remapped.Action;

        warnings.Add(
            $"{edgeDescription}: nenhuma action com title '{candidateTitle}' na collection destino; " +
            $"referência para '{originalTarget}' mantida.");
        return originalTarget;
    }

    /// <summary>Replaces the text up to (and including) the first "/" with <paramref name="prefix"/>.</summary>
    private static string ApplyTitlePrefix(string? title, string prefix)
    {
        if (title is null) return prefix;
        var slash = title.IndexOf('/');
        return slash >= 0 ? prefix + title[(slash + 1)..] : prefix + title;
    }

    /// <summary>
    /// "action_49668" -> "action_49668-2" -> "action_49668-3" ... Strips any existing
    /// numeric suffix first so re-cloning a clone still grows off the original root.
    /// </summary>
    private static string NextSuffixedId(string baseId, ISet<string> existingIds)
    {
        var root = RootActionId(baseId);
        for (var n = 2; ; n++)
        {
            var candidate = $"{root}-{n}";
            if (!existingIds.Contains(candidate)) return candidate;
        }
    }
}
