using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderProcessing.Domain.Exceptions;
using OrderProcessing.Shared.Responses;

namespace OrderProcessing.Api.Middleware;

public sealed class GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unhandled exception for {Method} {Path}", context.Request.Method, context.Request.Path);
            await WriteProblemAsync(context, ex);
        }
    }

    private static async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title) = exception switch
        {
            DomainException => (StatusCodes.Status400BadRequest, "Business rule violation"),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Concurrency conflict"),
            DbUpdateException => (StatusCodes.Status503ServiceUnavailable, "Database error"),
            _ => (StatusCodes.Status500InternalServerError, "Unhandled error")
        };

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = exception.Message,
            Instance = context.Request.Path
        };
        problem.Extensions["traceId"] = context.TraceIdentifier;
        problem.Extensions["response"] = ApiResponse<object>.Fail(title, [exception.Message], context.TraceIdentifier);

        await context.Response.WriteAsJsonAsync(problem);
    }
}
