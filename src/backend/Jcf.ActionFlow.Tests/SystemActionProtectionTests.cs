using Jcf.ActionFlow.Core.Copying;
using Jcf.ActionFlow.Core.Exceptions;
using Jcf.ActionFlow.Core.Repositories;
using Jcf.ActionFlow.Core.Services;
using Jcf.ActionFlow.Tests.Fixtures;

namespace Jcf.ActionFlow.Tests;

public class SystemActionProtectionTests
{
    [Theory]
    [InlineData("welcome")]
    [InlineData("fallback")]
    [InlineData("anything_else")]
    [InlineData("run_always")]
    public void System_actions_cannot_be_copied_or_moved(string systemActionId)
    {
        var workspace = SampleWorkspaceFixture.Load().Workspace;
        var service = new ActionCopyService();

        Assert.Throws<SystemActionProtectedException>(() =>
            service.Execute(workspace, systemActionId, new CopyActionRequest("prod", CopyModes.Copy, null, ReferenceStrategies.Keep)));
    }

    [Theory]
    [InlineData("welcome")]
    [InlineData("fallback")]
    [InlineData("anything_else")]
    [InlineData("run_always")]
    public void System_actions_cannot_be_deleted(string systemActionId)
    {
        var repository = new InMemoryWorkspaceRepository();
        var service = new WorkspaceService(repository, new ActionCopyService());
        var session = repository.Add(SampleWorkspaceFixture.Load());

        Assert.Throws<SystemActionProtectedException>(() => service.DeleteAction(session.Id, systemActionId, force: true));
    }

    [Theory]
    [InlineData("welcome")]
    [InlineData("fallback")]
    [InlineData("anything_else")]
    [InlineData("run_always")]
    public void System_actions_cannot_be_renamed(string systemActionId)
    {
        var repository = new InMemoryWorkspaceRepository();
        var service = new WorkspaceService(repository, new ActionCopyService());
        var session = repository.Add(SampleWorkspaceFixture.Load());

        Assert.Throws<SystemActionProtectedException>(() => service.RenameAction(session.Id, systemActionId, "Nova label"));
    }
}
