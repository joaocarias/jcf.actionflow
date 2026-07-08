namespace Jcf.ActionFlow.Core.Exceptions;

public sealed class WorkspaceNotFoundException(string sessionId)
    : Exception($"Workspace de sessão '{sessionId}' não encontrado.")
{
    public string SessionId { get; } = sessionId;
}

public sealed class InvalidWorkspaceException(string message) : Exception(message);

public sealed class ActionNotFoundException(string actionId)
    : Exception($"Action '{actionId}' não encontrada.")
{
    public string ActionId { get; } = actionId;
}

public sealed class SystemActionProtectedException(string actionId)
    : Exception($"A action de sistema '{actionId}' não pode ser copiada, movida ou excluída.")
{
    public string ActionId { get; } = actionId;
}

public sealed class ActionHasReferencesException(string actionId, IReadOnlyList<string> referencedBy)
    : Exception($"Action '{actionId}' é referenciada por {referencedBy.Count} ponto(s); use force=true para excluir mesmo assim.")
{
    public string ActionId { get; } = actionId;
    public IReadOnlyList<string> ReferencedBy { get; } = referencedBy;
}
