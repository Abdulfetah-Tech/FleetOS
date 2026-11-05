using System.Text.Json;
using EthioFleet.Domain.Common;
using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace EthioFleet.Api.Middleware;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title, detail) = exception switch
        {
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed", "One or more validation errors occurred."),
            DomainException => (StatusCodes.Status400BadRequest, "Domain rule violated", exception.Message),
            UnauthorizedAccessException => (StatusCodes.Status403Forbidden, "Forbidden", "The request is not authorized."),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict", "The resource was changed by another request."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Duplicate resource", "A record with the same unique identifier already exists."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred", "The server could not complete the request.")
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled request exception for {RequestId}", httpContext.TraceIdentifier);
        }
        else
        {
            logger.LogWarning("Request failed with {StatusCode} for {RequestId}", statusCode, httpContext.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };
        problem.Extensions["traceId"] = httpContext.TraceIdentifier;
        httpContext.Response.StatusCode = statusCode;

        var problemDetailsContext = new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problem
        };

        if (await problemDetailsService.TryWriteAsync(problemDetailsContext))
        {
            return true;
        }

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            cancellationToken);
        return true;
    }
}
