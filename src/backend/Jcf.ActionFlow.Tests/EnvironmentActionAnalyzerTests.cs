using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class EnvironmentActionAnalyzerTests
{
    [Fact]
    public void Sample_workspace_flags_hml_actions_with_no_prod_counterpart()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var missing = EnvironmentActionAnalyzer.FindMissingEnvironments(workspace);

        // "hml/triagem/finaliza" (action_49668) and "prod/triagem/finaliza" (action_49668-2)
        // share the same base name "triagem/finaliza" and exist in both envs: not flagged.
        Assert.DoesNotContain("action_49668", missing.Keys);
        Assert.DoesNotContain("action_49668-2", missing.Keys);

        // "hml/triagem/seleciona-cartão", "hml/triagem", and "hml/triagem/registra-tentativa"
        // have no "prod/..." counterpart with the same base name.
        Assert.Equal(["prod"], missing["action_25692"]);
        Assert.Equal(["prod"], missing["action_8087"]);
        Assert.Equal(["prod"], missing["action_8197"]);
    }

    [Fact]
    public void Same_base_name_present_in_every_environment_is_not_flagged()
    {
        var workspace = WorkspaceWith(
            ("a1", "hml/finaliza"),
            ("a2", "dev/finaliza"),
            ("a3", "prod/finaliza"));

        var missing = EnvironmentActionAnalyzer.FindMissingEnvironments(workspace);

        Assert.Empty(missing);
    }

    [Fact]
    public void Different_path_depth_is_not_considered_the_same_action()
    {
        var workspace = WorkspaceWith(
            ("a1", "hml/triagem/finaliza"),
            ("a2", "prod/finaliza"));

        var missing = EnvironmentActionAnalyzer.FindMissingEnvironments(workspace);

        Assert.Equal(["prod"], missing["a1"]);
        Assert.Equal(["hml"], missing["a2"]);
    }

    [Fact]
    public void System_actions_are_never_flagged()
    {
        var workspace = WorkspaceWith(("a1", "hml/finaliza"), ("a2", "prod/outra-coisa"));
        workspace.Actions.Add(new ActionDefinition { Action = "welcome", Title = "hml/welcome" });

        var missing = EnvironmentActionAnalyzer.FindMissingEnvironments(workspace);

        Assert.DoesNotContain("welcome", missing.Keys);
    }

    [Fact]
    public void Titles_without_a_slash_are_ignored()
    {
        var workspace = WorkspaceWith(("a1", "hml/finaliza"), ("a2", "prod/finaliza"), ("a3", "Greet customer"));

        var missing = EnvironmentActionAnalyzer.FindMissingEnvironments(workspace);

        Assert.DoesNotContain("a3", missing.Keys);
    }

    [Fact]
    public void Single_environment_workspace_has_nothing_to_compare_against()
    {
        var workspace = WorkspaceWith(("a1", "hml/finaliza"), ("a2", "hml/outra-coisa"));

        var missing = EnvironmentActionAnalyzer.FindMissingEnvironments(workspace);

        Assert.Empty(missing);
    }

    [Fact]
    public void Sample_workspace_has_no_step_count_mismatches()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var mismatches = EnvironmentActionAnalyzer.FindStepCountMismatches(workspace);

        // action_49668 (hml/triagem/finaliza) and action_49668-2 (prod/triagem/finaliza)
        // are the only pair sharing a base name across environments, and both have 1 step.
        Assert.Empty(mismatches);
    }

    [Fact]
    public void Different_step_count_for_the_same_base_name_across_envs_is_flagged()
    {
        var workspace = WorkspaceWithSteps(("a1", "hml/finaliza", 3), ("a2", "prod/finaliza", 5));

        var mismatches = EnvironmentActionAnalyzer.FindStepCountMismatches(workspace);

        Assert.Equal([new EnvironmentStepCount("prod", 5)], mismatches["a1"]);
        Assert.Equal([new EnvironmentStepCount("hml", 3)], mismatches["a2"]);
    }

    [Fact]
    public void Same_step_count_across_envs_is_not_flagged()
    {
        var workspace = WorkspaceWithSteps(("a1", "hml/finaliza", 2), ("a2", "prod/finaliza", 2));

        var mismatches = EnvironmentActionAnalyzer.FindStepCountMismatches(workspace);

        Assert.Empty(mismatches);
    }

    [Fact]
    public void Action_with_no_counterpart_in_any_environment_is_not_flagged()
    {
        var workspace = WorkspaceWithSteps(("a1", "hml/finaliza", 2));

        var mismatches = EnvironmentActionAnalyzer.FindStepCountMismatches(workspace);

        Assert.Empty(mismatches);
    }

    [Fact]
    public void Only_the_environment_that_diverges_is_reported()
    {
        var workspace = WorkspaceWithSteps(
            ("a1", "hml/finaliza", 2),
            ("a2", "dev/finaliza", 2),
            ("a3", "prod/finaliza", 5));

        var mismatches = EnvironmentActionAnalyzer.FindStepCountMismatches(workspace);

        Assert.Equal([new EnvironmentStepCount("prod", 5)], mismatches["a1"]);
        Assert.Equal([new EnvironmentStepCount("prod", 5)], mismatches["a2"]);
        Assert.Equal([new EnvironmentStepCount("dev", 2), new EnvironmentStepCount("hml", 2)], mismatches["a3"]);
    }

    private static WorkspaceData WorkspaceWith(params (string Id, string Title)[] actions) =>
        WorkspaceWithSteps(actions.Select(a => (a.Id, a.Title, StepCount: 0)).ToArray());

    private static WorkspaceData WorkspaceWithSteps(params (string Id, string Title, int StepCount)[] actions) =>
        new()
        {
            Actions = actions
                .Select(a => new ActionDefinition
                {
                    Action = a.Id,
                    Title = a.Title,
                    Steps = Enumerable.Range(0, a.StepCount).Select(i => new Step { StepId = $"s{i}" }).ToList(),
                })
                .ToList(),
        };
}
