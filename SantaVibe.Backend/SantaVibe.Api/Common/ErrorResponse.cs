namespace SantaVibe.Api.Common;

/// <summary>
/// Standard error response model for API errors
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// Error type/code (e.g., "ValidationError", "EmailAlreadyExists")
    /// </summary>
    public required string Error { get; set; }

    /// <summary>
    /// Human-readable error message
    /// </summary>
    public required string Message { get; set; }

    /// <summary>
    /// Detailed validation errors by field (optional)
    /// </summary>
    public Dictionary<string, string[]>? Details { get; set; }
}
