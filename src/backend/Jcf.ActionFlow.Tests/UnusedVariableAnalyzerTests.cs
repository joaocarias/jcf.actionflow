using System.Text.Json;
using Jcf.ActionFlow.Core.Domain;
using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class UnusedVariableAnalyzerTests
{
    [Fact]
    public void Sample_workspace_flags_a_skill_variable_that_is_written_but_never_read()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var unused = UnusedVariableAnalyzer.FindUnusedVariables(workspace);

        // action_8197's step_222 sets the skill variable "SelectedCard", but nothing in the
        // workspace ever reads it back.
        Assert.Contains("SelectedCard", unused["action_8197"]);
    }

    [Fact]
    public void Sample_workspace_does_not_flag_a_skill_variable_read_via_interpolation()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var unused = UnusedVariableAnalyzer.FindUnusedVariables(workspace);

        // action_8197's "TentativasCartao" is set via step_222's context assignment and read
        // back both by its own increment expression and as an operand in step_440's/step_280's
        // conditions.
        Assert.DoesNotContain("TentativasCartao", unused.GetValueOrDefault("action_8197", []));
    }

    [Fact]
    public void Local_step_and_result_variables_are_never_flagged_even_when_never_read()
    {
        // Watson auto-declares one local variable per step (its own "variable", or a resolver's
        // "result_variable"); almost none of them are ever read on purpose, so only global
        // (workspace.variables[]) state is worth an "unused" warning - see the sample
        // workspace's own action_49668 ("finaliza"), whose single step sets "step_798" and
        // never reads it.
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var unused = UnusedVariableAnalyzer.FindUnusedVariables(workspace);

        Assert.DoesNotContain("action_49668", unused.Keys);
    }

    [Fact]
    public void Skill_variable_written_in_one_action_and_read_in_another_is_not_flagged()
    {
        var writer = new ActionDefinition
        {
            Action = "writer",
            Steps = [WriteSkillVariable("s1", "Shared")],
        };
        var reader = new ActionDefinition
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
        };
        var workspace = new WorkspaceData
        {
            Actions = [writer, reader],
            Variables = [new VariableDeclaration { Variable = "Shared" }],
        };

        var unused = UnusedVariableAnalyzer.FindUnusedVariables(workspace);

        Assert.DoesNotContain("writer", unused.Keys);
    }

    [Fact]
    public void Skill_variable_never_read_anywhere_is_flagged_on_the_action_that_wrote_it()
    {
        var workspace = new WorkspaceData
        {
            Actions = [new ActionDefinition { Action = "a1", Steps = [WriteSkillVariable("s1", "Orphan")] }],
            Variables = [new VariableDeclaration { Variable = "Orphan" }],
        };

        var unused = UnusedVariableAnalyzer.FindUnusedVariables(workspace);

        Assert.Equal(["Orphan"], unused["a1"]);
    }

    [Fact]
    public void Skill_variable_used_as_a_condition_operand_counts_as_read()
    {
        var workspace = new WorkspaceData
        {
            Actions =
            [
                new ActionDefinition
                {
                    Action = "a1",
                    Steps =
                    [
                        WriteSkillVariable("s1", "Flag"),
                        new Step
                        {
                            StepId = "s2",
                            Resolver = new Resolver { Type = ResolverTypes.Continue },
                            Condition = ComparisonCondition("Flag"),
                        },
                    ],
                },
            ],
            Variables = [new VariableDeclaration { Variable = "Flag" }],
        };

        var unused = UnusedVariableAnalyzer.FindUnusedVariables(workspace);

        Assert.DoesNotContain("a1", unused.Keys);
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

    // A comparison condition shaped like Watson's "eq"/"neq"/"gte" operators, which fall
    // through to Condition.ExtensionData since they're dynamic keys (see Condition.cs).
    private static Condition ComparisonCondition(string skillVariableOperand) => new()
    {
        ExtensionData = new Dictionary<string, JsonElement>
        {
            ["eq"] = JsonDocument.Parse($$"""[{"skill_variable": "{{skillVariableOperand}}"}, {"scalar": true}]""").RootElement.Clone(),
        },
    };

    private static JsonElement Scalar(string rawValue) =>
        JsonDocument.Parse($$"""{"scalar": {{rawValue}}}""").RootElement.Clone();
}
