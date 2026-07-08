namespace Jcf.ActionFlow.Core.Domain;

/// <summary>
/// Watson Assistant Actions reserves these action ids for lifecycle hooks.
/// They don't belong to any collection and can't be copied, moved, or deleted.
/// </summary>
public static class SystemActions
{
    public const string Welcome = "welcome";
    public const string Fallback = "fallback";
    public const string AnythingElse = "anything_else";
    public const string RunAlways = "run_always";

    private static readonly HashSet<string> Ids = [Welcome, Fallback, AnythingElse, RunAlways];

    public static bool IsSystemAction(string actionId) => Ids.Contains(actionId);
}
