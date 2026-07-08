using System.Text.Json.Serialization;
using Jcf.ActionFlow.Api;
using Jcf.ActionFlow.Api.Endpoints;
using Jcf.ActionFlow.Core.Copying;
using Jcf.ActionFlow.Core.Repositories;
using Jcf.ActionFlow.Core.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<CoreExceptionHandler>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
    // Matches Core's WorkspaceJsonSerializer: don't emit nulls for fields that weren't
    // there. Also keeps polymorphic types (e.g. Condition's dynamic comparison-operator
    // keys) unambiguous for clients — a null "intent" alongside a real "neq" key would
    // otherwise look like two candidate operators.
    options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

builder.Services.AddSingleton<IWorkspaceRepository, InMemoryWorkspaceRepository>();
builder.Services.AddScoped<WorkspaceService>();
builder.Services.AddSingleton<ActionCopyService>();

const string FrontendCorsPolicy = "Frontend";
builder.Services.AddCors(options =>
{
    // No auth/multi-tenant boundary yet; every write is scoped to an opaque session id,
    // so a permissive dev-time policy is enough for now. Tighten before this leaves phase 1.
    options.AddPolicy(FrontendCorsPolicy, policy => policy
        .AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader());
});

var app = builder.Build();

app.UseExceptionHandler();
app.UseCors(FrontendCorsPolicy);

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.MapWorkspaceEndpoints();
app.MapActionEndpoints();

app.Run();

public partial class Program;
