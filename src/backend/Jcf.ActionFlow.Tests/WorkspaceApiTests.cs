using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Jcf.ActionFlow.Core.Copying;
using Jcf.ActionFlow.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Jcf.ActionFlow.Tests;

/// <summary>
/// End-to-end smoke test mirroring the curl workflow from the README: upload, graph,
/// copy, validate, export.
/// </summary>
public class WorkspaceApiTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Full_workflow_upload_graph_copy_validate_export()
    {
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(SampleWorkspaceFixture.RawJson));
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Add(fileContent, "file", "Lab-Chat-action.json");

        var importResponse = await _client.PostAsync("/api/workspaces", content);
        Assert.Equal(HttpStatusCode.Created, importResponse.StatusCode);

        var import = await importResponse.Content.ReadFromJsonAsync<JsonElement>();
        var sessionId = import.GetProperty("sessionId").GetString()!;
        Assert.Equal(9, import.GetProperty("summary").GetProperty("actionCount").GetInt32());
        Assert.Empty(import.GetProperty("issues").EnumerateArray());

        var graphResponse = await _client.GetAsync($"/api/workspaces/{sessionId}/graph?level=actions");
        graphResponse.EnsureSuccessStatusCode();
        var graph = await graphResponse.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(9, graph.GetProperty("nodes").GetArrayLength());

        var copyResponse = await _client.PostAsJsonAsync(
            $"/api/workspaces/{sessionId}/actions/action_8087/copy",
            new CopyActionRequest("prod", CopyModes.Move, null, ReferenceStrategies.Keep));
        copyResponse.EnsureSuccessStatusCode();

        var validateResponse = await _client.GetAsync($"/api/workspaces/{sessionId}/validate");
        validateResponse.EnsureSuccessStatusCode();

        var exportResponse = await _client.GetAsync($"/api/workspaces/{sessionId}/export");
        exportResponse.EnsureSuccessStatusCode();
        var exported = await exportResponse.Content.ReadAsStringAsync();
        using var exportedDoc = JsonDocument.Parse(exported);
        Assert.Equal("Lab-Chat-action", exportedDoc.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task Unknown_session_returns_404_problem_details()
    {
        var response = await _client.GetAsync("/api/workspaces/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
