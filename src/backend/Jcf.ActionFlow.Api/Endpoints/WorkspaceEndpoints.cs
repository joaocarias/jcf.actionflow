using Jcf.ActionFlow.Core.Graph;
using Jcf.ActionFlow.Core.Services;
using Jcf.ActionFlow.Core.Validation;

namespace Jcf.ActionFlow.Api.Endpoints;

public static class WorkspaceEndpoints
{
    public static void MapWorkspaceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/workspaces").WithTags("Workspaces");

        group.MapPost("/", async (IFormFile file, WorkspaceService service) =>
            {
                using var reader = new StreamReader(file.OpenReadStream());
                var json = await reader.ReadToEndAsync();

                var session = service.Import(json);
                var summary = service.GetSummary(session.Id);
                var issues = WorkspaceValidator.Validate(session.Export.Workspace);

                return Results.Created(
                    $"/api/workspaces/{session.Id}",
                    new ImportResponse(session.Id, summary, issues));
            })
            .Accepts<IFormFile>("multipart/form-data")
            .DisableAntiforgery()
            .WithName("ImportWorkspace")
            .WithSummary("Sobe um JSON exportado do Watson Assistant (Actions) e abre uma sessão de edição.");

        group.MapGet("/{id}", (string id, WorkspaceService service) =>
                Results.Ok(service.GetSummary(id)))
            .WithName("GetWorkspaceSummary");

        group.MapGet("/{id}/export", (string id, WorkspaceService service) =>
                Results.Content(service.ExportJson(id), "application/json"))
            .WithName("ExportWorkspace")
            .WithSummary("Devolve o JSON completo, pronto para reimportar no Watson.");

        group.MapGet("/{id}/collections", (string id, WorkspaceService service) =>
                Results.Ok(service.GetCollections(id)))
            .WithName("GetCollections");

        group.MapGet("/{id}/actions", (string id, WorkspaceService service) =>
                Results.Ok(service.GetActions(id)))
            .WithName("GetActions");

        group.MapGet("/{id}/variables", (string id, WorkspaceService service) =>
                Results.Ok(service.GetVariables(id)))
            .WithName("GetVariables")
            .WithSummary("Lista workspace.variables[] com onde cada uma é atribuída (set) e usada (get).");

        group.MapGet("/{id}/actions/{actionId}", (string id, string actionId, WorkspaceService service) =>
                Results.Ok(service.GetActionDetail(id, actionId)))
            .WithName("GetActionDetail");

        group.MapGet("/{id}/graph", (string id, WorkspaceService service, string level = "actions") =>
            {
                var workspace = service.GetSession(id).Export.Workspace;
                var graph = level.ToLowerInvariant() switch
                {
                    "steps" => StepGraphBuilder.Build(workspace),
                    _ => ActionGraphBuilder.Build(workspace),
                };
                return Results.Ok(graph);
            })
            .WithName("GetGraph")
            .WithSummary("level=actions (padrão) ou level=steps.");

        group.MapGet("/{id}/validate", (string id, WorkspaceService service) =>
                Results.Ok(WorkspaceValidator.Validate(service.GetSession(id).Export.Workspace)))
            .WithName("ValidateWorkspace");
    }
}
