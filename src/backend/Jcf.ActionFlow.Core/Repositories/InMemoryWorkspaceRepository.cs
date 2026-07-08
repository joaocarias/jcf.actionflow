using System.Collections.Concurrent;
using Jcf.ActionFlow.Core.Models;

namespace Jcf.ActionFlow.Core.Repositories;

public sealed class InMemoryWorkspaceRepository : IWorkspaceRepository
{
    private readonly ConcurrentDictionary<string, WorkspaceSession> _sessions = new();

    public WorkspaceSession Add(WorkspaceExport export)
    {
        var now = DateTimeOffset.UtcNow;
        var session = new WorkspaceSession
        {
            Id = Guid.NewGuid().ToString("n"),
            Export = export,
            CreatedAt = now,
            UpdatedAt = now,
        };
        _sessions[session.Id] = session;
        return session;
    }

    public WorkspaceSession? Get(string id) => _sessions.GetValueOrDefault(id);

    public void Update(WorkspaceSession session)
    {
        session.UpdatedAt = DateTimeOffset.UtcNow;
        _sessions[session.Id] = session;
    }

    public bool Remove(string id) => _sessions.TryRemove(id, out _);
}
