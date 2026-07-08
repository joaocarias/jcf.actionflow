using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Repositories;

public sealed class WorkspaceSession
{
    public required string Id { get; init; }
    public required WorkspaceExport Export { get; set; }
    public required DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset UpdatedAt { get; set; }
}

/// <summary>
/// Storage for imported workspaces. In-memory for now (see <see cref="InMemoryWorkspaceRepository"/>);
/// swapping to a persistent store later only requires a new implementation of this interface.
/// </summary>
public interface IWorkspaceRepository
{
    WorkspaceSession Add(WorkspaceExport export);
    WorkspaceSession? Get(string id);
    void Update(WorkspaceSession session);
    bool Remove(string id);
}
