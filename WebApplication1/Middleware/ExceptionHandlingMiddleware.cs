using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace API.Middleware;

/// <summary>
/// Global exception handling middleware.
/// Catches unhandled exceptions and returns standard RFC 7807 ProblemDetails JSON.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate                     _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _log;
    private readonly IHostEnvironment                    _env;

    public ExceptionHandlingMiddleware(
        RequestDelegate next,
        ILogger<ExceptionHandlingMiddleware> log,
        IHostEnvironment env)
    {
        _next = next;
        _log  = log;
        _env  = env;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var traceId = Activity.Current?.Id ?? context.TraceIdentifier;
            _log.LogError(ex, "Unhandled exception on {Path}. TraceId: {TraceId}",
                context.Request.Path, traceId);

            var (status, title) = ex switch
            {
                DbUpdateConcurrencyException => (409, "Changed by someone else. Reload and retry."),
                UnauthorizedAccessException  => (403, "You do not have access to this resource."),
                KeyNotFoundException         => (404, "Not found."),
                ArgumentException arg        => (400, arg.Message),
                _                            => (500, "An unexpected error occurred.")
            };

            var problem = new ProblemDetails { Status = status, Title = title };
            problem.Extensions["traceId"] = traceId;

            if (_env.IsDevelopment())
                problem.Detail = ex.ToString();

            context.Response.StatusCode  = status;
            context.Response.ContentType = "application/problem+json";
            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}
