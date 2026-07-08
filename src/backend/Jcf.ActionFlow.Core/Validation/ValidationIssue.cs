namespace Jcf.ActionFlow.Core.Validation;

public enum IssueSeverity
{
    Warning,
    Error,
}

public sealed record ValidationIssue(
    IssueSeverity Severity,
    string Code,
    string Message,
    string? ActionId = null,
    string? StepId = null);
