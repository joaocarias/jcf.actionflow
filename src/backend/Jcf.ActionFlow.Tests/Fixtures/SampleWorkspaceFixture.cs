using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Serialization;

namespace Jcf.ActionFlow.Tests.Fixtures;

/// <summary>
/// Loads the real Watson Assistant export used as the golden fixture across the test suite.
/// Copied next to the test binaries by the csproj (see Content/Link in the .csproj).
/// </summary>
public static class SampleWorkspaceFixture
{
    private const string FileName = "samples/Lab-Chat-action.json";

    public static string RawJson { get; } = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, FileName));

    public static WorkspaceExport Load() => WorkspaceJsonSerializer.Parse(RawJson);
}
