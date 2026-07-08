using Jcf.ActionFlow.Core.Models;
using Jcf.ActionFlow.Core.Validation;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class WorkspaceValidatorTests
{
    [Fact]
    public void The_sample_workspace_has_no_issues()
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;

        var issues = WorkspaceValidator.Validate(workspace);

        Assert.Empty(issues);
    }

    [Fact]
    public void Every_kind_of_synthetic_defect_is_detected()
    {
        var workspace = BuildWorkspaceWithSyntheticDefects();

        var issues = WorkspaceValidator.Validate(workspace);

        Assert.Contains(issues, i => i.Code == "step.next_step.dangling" && i.ActionId == "a1" && i.StepId == "s1");
        Assert.Contains(issues, i => i.Code == "action.next_action.dangling" && i.ActionId == "a1");
        Assert.Contains(issues, i => i.Code == "step.invoke.dangling" && i.ActionId == "a1" && i.StepId == "s2");
        Assert.Contains(issues, i => i.Code == "action.condition.intent.missing" && i.ActionId == "a3");
        Assert.Contains(issues, i => i.Code == "collection.action_reference.dangling");
        Assert.Contains(issues, i => i.Code == "collection.action_reference.duplicate" && i.ActionId == "a1");
        Assert.Contains(issues, i => i.Code == "step.variable.undeclared" && i.ActionId == "a1" && i.StepId == "s1");
        Assert.Contains(issues, i => i.Code == "intent.orphan");
        Assert.Contains(issues, i =>
            i.Code == "step.invoke.cross_collection" && i.Severity == IssueSeverity.Warning && i.ActionId == "a2");
    }

    private static WorkspaceData BuildWorkspaceWithSyntheticDefects()
    {
        var a1 = new ActionDefinition
        {
            Action = "a1",
            Title = "a1",
            Condition = new Condition { Intent = "a1_intent" },
            NextAction = "ghost_action",
            Variables = [new VariableDeclaration { Variable = "s1" }],
            Steps =
            [
                new Step
                {
                    StepId = "s1",
                    Variable = "s1",
                    Condition = new Condition { Expression = "${undeclared_var} == true" },
                    NextStep = "s_missing",
                    Resolver = new Resolver { Type = ResolverTypes.Continue },
                },
                new Step
                {
                    StepId = "s2",
                    Variable = "s2",
                    Resolver = new Resolver
                    {
                        Type = ResolverTypes.InvokeAnotherAction,
                        InvokeAction = new InvokeAction { Action = "ghost_action_2" },
                    },
                },
            ],
        };

        var a2 = new ActionDefinition
        {
            Action = "a2",
            Title = "a2",
            Condition = new Condition { Intent = "a2_intent" },
            Steps =
            [
                new Step
                {
                    StepId = "s1",
                    Variable = "s1",
                    Resolver = new Resolver
                    {
                        Type = ResolverTypes.InvokeAnotherAction,
                        InvokeAction = new InvokeAction { Action = "a1" },
                    },
                },
            ],
        };

        var a3 = new ActionDefinition
        {
            Action = "a3",
            Title = "a3",
            Condition = new Condition { Intent = "a3_missing_intent" },
        };

        return new WorkspaceData
        {
            Actions = [a1, a2, a3],
            Intents =
            [
                new Intent { Name = "a1_intent" },
                new Intent { Name = "a2_intent" },
                new Intent { Name = "action_orphan_intent" },
            ],
            Variables = [],
            Collections =
            [
                new Collection
                {
                    CollectionId = "col1",
                    Title = "hml",
                    ActionReferences =
                    [
                        new ActionReference { Action = "a1" },
                        new ActionReference { Action = "ghost_ref_action" },
                        new ActionReference { Action = "a3" },
                    ],
                },
                new Collection
                {
                    CollectionId = "col2",
                    Title = "prod",
                    ActionReferences =
                    [
                        new ActionReference { Action = "a2" },
                        new ActionReference { Action = "a1" },
                    ],
                },
            ],
        };
    }
}
