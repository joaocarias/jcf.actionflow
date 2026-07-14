namespace Jcf.ActionFlow.Core.Domain;

public sealed record WorkspaceSummary(
    string SessionId,
    string? Name,
    int ActionCount,
    int BusinessActionCount,
    int SystemActionCount,
    int IntentCount,
    int VariableCount,
    int CollectionCount);

public sealed record CollectionActionSummary(
    string ActionId,
    string? Title,
    int StepCount,
    IReadOnlyList<string> MissingInEnvironments,
    IReadOnlyList<EnvironmentStepCount> StepCountMismatches,
    IReadOnlyList<string> UnusedVariables);

public sealed record CollectionSummary(
    string CollectionId,
    string? Title,
    IReadOnlyList<CollectionActionSummary> Actions);

public sealed record ActionSummary(
    string ActionId,
    string? Title,
    bool IsSystemAction,
    bool IsOrphan,
    string? CollectionId,
    int StepCount,
    IReadOnlyList<string> MissingInEnvironments,
    IReadOnlyList<EnvironmentStepCount> StepCountMismatches,
    IReadOnlyList<string> UnusedVariables);

public sealed record DeleteActionResult(string ActionId, IReadOnlyList<string> OrphanedReferences);
