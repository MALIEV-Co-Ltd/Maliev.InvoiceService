namespace Maliev.InvoiceService.Api.Models.Common;

/// <summary>
/// Represents a standardized error response for API operations.
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Gets or sets the HTTP status code.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the error message.
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional error details (only included in development/testing environments).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Gets or sets the trace identifier for correlating logs.
    /// </summary>
    public string? TraceId { get; set; }

    /// <summary>
    /// Gets or sets validation errors grouped by field name.
    /// </summary>
    public Dictionary<string, string[]>? Errors { get; set; }
}
