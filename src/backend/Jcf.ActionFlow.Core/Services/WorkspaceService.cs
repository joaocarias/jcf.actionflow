using Jcf.ActionFlow.Core.Copying;
using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Exceptions;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Repositories;
using Jcf.ActionFlow.Core.Serialization;

namespace Jcf.ActionFlow.Core.Services;

/// <summary>
/// Orchestrates import/export and the basic read/edit operations over a workspace session.
/// Graph construction and validation live in their own services; copy/move's algorithm lives
/// in <see cref="ActionCopyService"/> but is persisted here, same as every other edit.
/// </summary>
public sealed class WorkspaceService(IWorkspaceRepository repository, ActionCopyService copyService)
{
    public WorkspaceSession Import(string json)
    {
        WorkspaceExport export;
        try
        {
            export = WorkspaceJsonSerializer.Parse(json);
        }
        catch (System.Text.Json.JsonException ex)
        {
            throw new InvalidWorkspaceException($"JSON inválido: {ex.Message}");
        }

        if (export.Workspace.Actions.Count == 0)
        {
            throw new InvalidWorkspaceException("O workspace não contém nenhuma action.");
        }

        return repository.Add(export);
    }

    public WorkspaceSession GetSession(string id) =>
        repository.Get(id) ?? throw new WorkspaceNotFoundException(id);

    public WorkspaceSummary GetSummary(string id)
    {
        var export = GetSession(id).Export;
        var actions = export.Workspace.Actions;
        var systemCount = actions.Count(a => Domain.SystemActions.IsSystemAction(a.Action));

        return new WorkspaceSummary(
            id,
            export.Name,
            actions.Count,
            actions.Count - systemCount,
            systemCount,
            export.Workspace.Intents.Count,
            export.Workspace.Variables.Count,
            export.Workspace.Collections.Count);
    }

    public string ExportJson(string id) => WorkspaceJsonSerializer.Serialize(GetSession(id).Export);

    public IReadOnlyList<CollectionSummary> GetCollections(string id)
    {
        var workspace = GetSession(id).Export.Workspace;
        var actionsById = WorkspaceLookups.ActionsById(workspace);

        return workspace.Collections
            .Select(c => new CollectionSummary(
                c.CollectionId,
                c.Title,
                c.ActionReferences
                    .Select(r => actionsById.TryGetValue(r.Action, out var action)
                        ? new CollectionActionSummary(action.Action, action.Title, action.Steps.Count)
                        : new CollectionActionSummary(r.Action, null, 0))
                    .ToList()))
            .ToList();
    }

    public IReadOnlyList<ActionSummary> GetActions(string id)
    {
        var workspace = GetSession(id).Export.Workspace;
        var collectionByAction = WorkspaceLookups.CollectionIdByAction(workspace);

        return workspace.Actions
            .Select(a =>
            {
                var isSystem = Domain.SystemActions.IsSystemAction(a.Action);
                var collectionId = collectionByAction.GetValueOrDefault(a.Action);
                return new ActionSummary(
                    a.Action,
                    a.Title,
                    isSystem,
                    IsOrphan: !isSystem && collectionId is null,
                    collectionId,
                    a.Steps.Count);
            })
            .ToList();
    }

    public ActionDefinition GetActionDetail(string id, string actionId) =>
        WorkspaceLookups.FindAction(GetSession(id).Export.Workspace, actionId);

    public CopyActionResult CopyOrMoveAction(string id, string actionId, CopyActionRequest request)
    {
        var session = GetSession(id);
        var result = copyService.Execute(session.Export.Workspace, actionId, request);
        repository.Update(session);
        return result;
    }

    public ActionDefinition RenameAction(string id, string actionId, string newTitle)
    {
        GuardNotSystemAction(actionId);
        var session = GetSession(id);
        var action = WorkspaceLookups.FindAction(session.Export.Workspace, actionId);
        action.Title = newTitle;
        repository.Update(session);
        return action;
    }

    public DeleteActionResult DeleteAction(string id, string actionId, bool force)
    {
        GuardNotSystemAction(actionId);
        var session = GetSession(id);
        var workspace = session.Export.Workspace;
        var action = WorkspaceLookups.FindAction(workspace, actionId);

        var referencedBy = WorkspaceLookups.FindReferences(workspace, actionId);
        if (referencedBy.Count > 0 && !force)
        {
            throw new ActionHasReferencesException(actionId, referencedBy);
        }

        workspace.Actions.Remove(action);

        if (action.Condition?.Intent is { } intentName)
        {
            workspace.Intents.RemoveAll(i => i.Name == intentName);
        }

        foreach (var collection in workspace.Collections)
        {
            collection.ActionReferences.RemoveAll(r => r.Action == actionId);
        }

        repository.Update(session);
        return new DeleteActionResult(actionId, referencedBy);
    }

    private static void GuardNotSystemAction(string actionId)
    {
        if (Domain.SystemActions.IsSystemAction(actionId))
        {
            throw new SystemActionProtectedException(actionId);
        }
    }
}
