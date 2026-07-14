using System.Text.Json;
using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class VariableUsageAnalyzerTests
{
    [Fact]
    public void Sample_workspace_lists_every_workspace_variable_exactly_once()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var usages = VariableUsageAnalyzer.Analyze(workspace);

        Assert.Equal(workspace.Variables.Count, usages.Count);
        Assert.Equal(
            workspace.Variables.Select(v => v.Variable).OrderBy(n => n, StringComparer.Ordinal),
            usages.Select(u => u.Variable));
    }

    [Fact]
    public void Sample_workspace_flags_SelectedCard_as_set_but_never_used()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var usages = VariableUsageAnalyzer.Analyze(workspace);
        var selectedCard = usages.Single(u => u.Variable == "SelectedCard");

        Assert.True(selectedCard.IsSet);
        Assert.Equal(["action_8197"], selectedCard.SetInActions);
        Assert.False(selectedCard.HasDefaultValue);
        Assert.False(selectedCard.IsUsed);
        Assert.Empty(selectedCard.UsedInActions);
    }

    [Fact]
    public void Sample_workspace_shows_ENV_as_both_set_and_used()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var usages = VariableUsageAnalyzer.Analyze(workspace);
        var env = usages.Single(u => u.Variable == "ENV");

        Assert.True(env.IsSet);
        Assert.True(env.IsUsed);
    }

    [Fact]
    public void Variable_set_via_context_assignment_and_read_via_interpolation_elsewhere_is_set_and_used()
    {
        var workspace = new WorkspaceData
        {
            Actions =
            [
                new ActionDefinition { Action = "writer", Steps = [WriteSkillVariable("s1", "Shared")] },
                new ActionDefinition
                {
                    Action = "reader",
                    Steps =
                    [
                        new Step
                        {
                            StepId = "s1",
                            Resolver = new Resolver { Type = ResolverTypes.Continue },
                            Condition = new Condition { Expression = "${Shared} == 1" },
                        },
                    ],
                },
            ],
            Variables = [new VariableDeclaration { Variable = "Shared" }],
        };

        var usage = VariableUsageAnalyzer.Analyze(workspace).Single();

        Assert.True(usage.IsSet);
        Assert.Equal(["writer"], usage.SetInActions);
        Assert.False(usage.HasDefaultValue);
        Assert.True(usage.IsUsed);
        Assert.Equal(["reader"], usage.UsedInActions);
    }

    [Fact]
    public void Variable_only_declared_with_no_step_assignment_or_read_is_neither_set_nor_used()
    {
        var workspace = new WorkspaceData { Variables = [new VariableDeclaration { Variable = "Orphan" }] };

        var usage = VariableUsageAnalyzer.Analyze(workspace).Single();

        Assert.False(usage.IsSet);
        Assert.Empty(usage.SetInActions);
        Assert.False(usage.HasDefaultValue);
        Assert.False(usage.IsUsed);
        Assert.Empty(usage.UsedInActions);
    }

    [Fact]
    public void A_declaration_with_initial_value_counts_as_set_even_without_a_step_assignment()
    {
        var declaration = new VariableDeclaration
        {
            Variable = "Constant",
            ExtensionData = new Dictionary<string, JsonElement>
            {
                ["initial_value"] = JsonDocument.Parse("""{"expression": "'HML'"}""").RootElement.Clone(),
            },
        };
        var workspace = new WorkspaceData { Variables = [declaration] };

        var usage = VariableUsageAnalyzer.Analyze(workspace).Single();

        Assert.True(usage.IsSet);
        Assert.Empty(usage.SetInActions);
        Assert.True(usage.HasDefaultValue);
    }

    private static Step WriteSkillVariable(string stepId, string skillVariable) => new()
    {
        StepId = stepId,
        Resolver = new Resolver { Type = ResolverTypes.Continue },
        Context = new StepContext
        {
            Variables = [new ContextVariableAssignment { SkillVariable = skillVariable, Value = Scalar("1") }],
        },
    };

    private static JsonElement Scalar(string rawValue) =>
        JsonDocument.Parse($$"""{"scalar": {{rawValue}}}""").RootElement.Clone();
}
