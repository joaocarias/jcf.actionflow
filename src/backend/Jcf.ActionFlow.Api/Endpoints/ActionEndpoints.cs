using Jcf.ActionFlow.Core.Copying;
using Jcf.ActionFlow.Core.Services;
using Jcf.ActionFlow.Core.Validation;

namespace Jcf.ActionFlow.Api.Endpoints;

public static class ActionEndpoints
{
    public static void MapActionEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workspaces/{id}/actions/{actionId}").WithTags("Actions");

        group.MapPost("/copy", (string id, string actionId, CopyActionRequest request, WorkspaceService service) =>
            {
                var result = service.CopyOrMoveAction(id, actionId, request);
                var issues = WorkspaceValidator.Validate(service.GetSession(id).Export.Workspace);
                return Results.Ok(new CopyActionResponse(result.Action, result.Warnings, issues));
            })
            .WithName("CopyOrMoveAction")
            .WithSummary("mode=copy clona a action (e o intent) para targetCollection; mode=move só realoca a referência.");

        group.MapPatch("/", (string id, string actionId, RenameActionRequest request, WorkspaceService service) =>
            {
                var action = service.RenameAction(id, actionId, request.Title);
                var issues = WorkspaceValidator.Validate(service.GetSession(id).Export.Workspace);
                return Results.Ok(new ActionWriteResponse(action, issues));
            })
            .WithName("RenameAction");

        group.MapDelete("/", (string id, string actionId, WorkspaceService service, bool force = false) =>
            {
                var result = service.DeleteAction(id, actionId, force);
                var issues = WorkspaceValidator.Validate(service.GetSession(id).Export.Workspace);
                return Results.Ok(new DeleteActionResponse(result.ActionId, result.OrphanedReferences, issues));
            })
            .WithName("DeleteAction");
    }
}
