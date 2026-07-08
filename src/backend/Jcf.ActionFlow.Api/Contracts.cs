using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Validation;

namespace Jcf.ActionFlow.Api;

public sealed record ImportResponse(string SessionId, WorkspaceSummary Summary, IReadOnlyList<ValidationIssue> Issues);

public sealed record RenameActionRequest(string Title);

public sealed record ActionWriteResponse(ActionDefinition Action, IReadOnlyList<ValidationIssue> Issues);

public sealed record CopyActionResponse(
    ActionDefinition Action,
    IReadOnlyList<string> Warnings,
    IReadOnlyList<ValidationIssue> Issues);

public sealed record DeleteActionResponse(
    string ActionId,
    IReadOnlyList<string> OrphanedReferences,
    IReadOnlyList<ValidationIssue> Issues);
