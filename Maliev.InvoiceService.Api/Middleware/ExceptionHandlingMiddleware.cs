using Maliev.InvoiceService.Api.Models.Common;
using System.Net;
using System.Text.Json;

namespace Maliev.InvoiceService.Api.Middleware;

/// <summary>
/// Global exception handling middleware that catches unhandled exceptions and returns standardized error responses.
/// </summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private readonly IWebHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="ExceptionHandlingMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">The logger for recording exception details.</param>
    /// <param name="environment">The web host environment for determining detail exposure.</param>
    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Processes an HTTP request and catches any unhandled exceptions, converting them to appropriate HTTP error responses.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex, _environment);
        }
    }

    /// <summary>
    /// Handles exceptions by creating appropriate error responses based on exception type and environment.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <param name="exception">The exception that was thrown.</param>
    /// <param name="environment">The web host environment for determining detail exposure.</param>
    private static async Task HandleExceptionAsync(HttpContext context, Exception exception, IWebHostEnvironment environment)
    {
        context.Response.ContentType = "application/json";

        var errorResponse = exception switch
        {
            ArgumentException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.BadRequest,
                Message = exception.Message,
                TraceId = context.TraceIdentifier,
                Details = environment.IsDevelopment() || environment.IsEnvironment("Testing") ? exception.ToString() : null
            },
            InvalidOperationException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Conflict,
                Message = exception.Message,
                TraceId = context.TraceIdentifier,
                Details = environment.IsDevelopment() || environment.IsEnvironment("Testing") ? exception.ToString() : null
            },
            UnauthorizedAccessException => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.Forbidden,
                Message = "Access denied",
                TraceId = context.TraceIdentifier,
                Details = environment.IsDevelopment() || environment.IsEnvironment("Testing") ? exception.ToString() : null
            },
            _ => new ErrorResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Message = environment.IsDevelopment() || environment.IsEnvironment("Testing")
                    ? exception.Message
                    : "An internal server error occurred",
                TraceId = context.TraceIdentifier,
                Details = environment.IsDevelopment() || environment.IsEnvironment("Testing") ? exception.ToString() : null
            }
        };

        context.Response.StatusCode = errorResponse.StatusCode;

        var options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(errorResponse, options));
    }
}
