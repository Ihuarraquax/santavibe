namespace SantaVibe.Api.Services;

/// <summary>
/// Service for validating and executing Secret Santa draw algorithm
/// </summary>
public interface IDrawAlgorithmService
{
    /// <summary>
    /// Validates whether a valid assignment graph can be created with given constraints
    /// </summary>
    /// <param name="participantIds">List of participant user IDs</param>
    /// <param name="exclusionPairs">List of exclusion pairs (userId1, userId2)</param>
    /// <returns>Validation result with errors if invalid</returns>
    DrawValidationResult ValidateDrawFeasibility(
        List<string> participantIds,
        List<(string userId1, string userId2)> exclusionPairs);

    /// <summary>
    /// Executes the draw algorithm to generate assignments
    /// </summary>
    /// <param name="participantIds">List of participant user IDs</param>
    /// <param name="exclusionPairs">List of exclusion pairs (userId1, userId2)</param>
    /// <returns>Dictionary mapping each Santa to their recipient</returns>
    /// <exception cref="DrawAlgorithmException">Thrown when algorithm fails to find valid assignments</exception>
    Dictionary<string, string> ExecuteDrawAlgorithm(
        List<string> participantIds,
        List<(string userId1, string userId2)> exclusionPairs);
}

/// <summary>
/// Result of draw feasibility validation
/// </summary>
public sealed record DrawValidationResult(
    bool IsValid,
    List<string> Errors);

/// <summary>
/// Exception thrown when draw algorithm fails
/// </summary>
public class DrawAlgorithmException : Exception
{
    public DrawAlgorithmException(string message) : base(message)
    {
    }

    public DrawAlgorithmException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
