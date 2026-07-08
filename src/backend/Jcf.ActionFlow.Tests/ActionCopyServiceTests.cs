using Jcf.ActionFlow.Core.Copying;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class ActionCopyServiceTests
{
    private readonly ActionCopyService _service = new();

    private static WorkspaceData FreshWorkspace() => SampleWorkspaceFixture.Load().Workspace;

    [Fact]
    public void Move_only_relocates_the_action_reference_without_cloning()
    {
        var workspace = FreshWorkspace();

        var result = _service.Execute(workspace, "action_8087", new CopyActionRequest("prod", CopyModes.Move, null, ReferenceStrategies.Keep));

        Assert.Empty(result.Warnings);
        Assert.Same(workspace.Actions.Single(a => a.Action == "action_8087"), result.Action);
        Assert.DoesNotContain(workspace.Collections.Single(c => c.Title == "hml").ActionReferences, r => r.Action == "action_8087");
        Assert.Contains(workspace.Collections.Single(c => c.Title == "prod").ActionReferences, r => r.Action == "action_8087");
        // still a single action_8087 in the whole workspace — nothing was cloned.
        Assert.Single(workspace.Actions, a => a.Action == "action_8087");
    }

    [Fact]
    public void Copy_with_keep_clones_action_and_intent_with_a_unique_suffix_and_flags_cross_collection_refs()
    {
        var workspace = FreshWorkspace();

        var result = _service.Execute(
            workspace,
            "action_49668",
            new CopyActionRequest("prod", CopyModes.Copy, "prod/", ReferenceStrategies.Keep));

        // "-2" is already taken (action_49668-2 exists in the fixture), so this must be "-3".
        Assert.Equal("action_49668-3", result.Action.Action);
        Assert.Equal("action_49668_intent_40724-3", result.Action.Condition?.Intent);
        Assert.Contains(workspace.Intents, i => i.Name == "action_49668_intent_40724-3");

        Assert.Equal("prod/triagem/finaliza", result.Action.Title);

        // next_action ("action_8197") is kept pointing at hml, so it must be flagged.
        Assert.Equal("action_8197", result.Action.NextAction);
        Assert.Contains(result.Warnings, w => w.Contains("action_8197"));

        Assert.Contains(workspace.Collections.Single(c => c.Title == "prod").ActionReferences, r => r.Action == "action_49668-3");

        // local variables travel with the clone, unrenamed.
        Assert.Contains(result.Action.Variables, v => v.Variable == "step_798");

        // original action is untouched.
        var original = workspace.Actions.Single(a => a.Action == "action_49668");
        Assert.Equal("hml/triagem/finaliza", original.Title);
        Assert.Equal("action_49668_intent_40724", original.Condition?.Intent);
    }

    [Fact]
    public void Copy_with_remap_repoints_references_that_have_a_same_titled_action_in_the_target_collection()
    {
        var workspace = FreshWorkspace();

        // action_8197's step_440 invokes action_49668 ("hml/triagem/finaliza"), and prod
        // already has "prod/triagem/finaliza" (action_49668-2) — that one should remap.
        // Its step_280 invokes action_25692, which has no prod counterpart — that one
        // should stay put, with a warning.
        var result = _service.Execute(
            workspace,
            "action_8197",
            new CopyActionRequest("prod", CopyModes.Copy, null, ReferenceStrategies.Remap));

        Assert.Equal("action_8197-2", result.Action.Action);

        var step440 = result.Action.Steps.Single(s => s.StepId == "step_440");
        Assert.Equal("action_49668-2", step440.Resolver.InvokeAction?.Action);

        var step280 = result.Action.Steps.Single(s => s.StepId == "step_280");
        Assert.Equal("action_25692", step280.Resolver.InvokeAction?.Action);

        Assert.Equal("action_25692", result.Action.NextAction);
        Assert.Contains(result.Warnings, w => w.Contains("action_25692"));
    }
}
