using Jcf.ActionFlow.Core.Serialization;
using Jcf.ActionFlow.Tests.Fixtures;
using Jcf.ActionFlow.Tests.Support;

namespace Jcf.ActionFlow.Tests;

public class RoundTripTests
{
    [Fact]
    public void Deserializing_then_serializing_the_sample_export_loses_nothing()
    {
        var export = SampleWorkspaceFixture.Load();

        var roundTripped = WorkspaceJsonSerializer.Serialize(export);

        JsonAssert.DeepEqual(SampleWorkspaceFixture.RawJson, roundTripped);
    }

    [Fact]
    public void Serializing_twice_is_stable()
    {
        var export = SampleWorkspaceFixture.Load();
        var first = WorkspaceJsonSerializer.Serialize(export);
        var reparsed = WorkspaceJsonSerializer.Parse(first);
        var second = WorkspaceJsonSerializer.Serialize(reparsed);

        JsonAssert.DeepEqual(first, second);
    }

    [Fact]
    public void Accented_pt_br_text_is_not_escaped()
    {
        var export = SampleWorkspaceFixture.Load();

        var json = WorkspaceJsonSerializer.Serialize(export);

        Assert.Contains("seleciona-cartão", json);
        Assert.DoesNotContain("\\u00e3", json);
    }
}
