using Jcf.ActionFlow.Core.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Jcf.ActionFlow.Api;

/// <summary>
/// Maps Core's domain exceptions to ProblemDetails responses, so endpoints stay free of
/// try/catch and every error shape is consistent.
/// </summary>
public sealed class CoreExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<CoreExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            WorkspaceNotFoundException => (StatusCodes.Status404NotFound, "Workspace não encontrado"),
            ActionNotFoundException => (StatusCodes.Status404NotFound, "Action não encontrada"),
            InvalidWorkspaceException => (StatusCodes.Status400BadRequest, "Workspace inválido"),
            SystemActionProtectedException => (StatusCodes.Status409Conflict, "Action de sistema protegida"),
            ActionHasReferencesException => (StatusCodes.Status409Conflict, "Action possui referências"),
            _ => (StatusCodes.Status500InternalServerError, "Erro interno"),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Erro não tratado processando {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
        };

        if (exception is ActionHasReferencesException referencesException)
        {
            problemDetails.Extensions["referencedBy"] = referencesException.ReferencedBy;
        }

        await problemDetailsService.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception,
        });
        return true;
    }
}
