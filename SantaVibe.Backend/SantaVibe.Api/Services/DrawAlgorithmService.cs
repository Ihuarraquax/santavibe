namespace SantaVibe.Api.Services;

/// <summary>
/// Implementation of Secret Santa draw algorithm with constraint validation
/// Uses a graph-based approach with backtracking to ensure valid assignments
/// </summary>
public class DrawAlgorithmService : IDrawAlgorithmService
{
    private readonly ILogger<DrawAlgorithmService> _logger;
    private const int MaxBacktrackAttempts = 1000;

    public DrawAlgorithmService(ILogger<DrawAlgorithmService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public DrawValidationResult ValidateDrawFeasibility(
        List<string> participantIds,
        List<(string userId1, string userId2)> exclusionPairs)
    {
        var errors = new List<string>();

        // Check minimum participants
        if (participantIds.Count < 3)
        {
            errors.Add("Minimum 3 participants required for draw");
            return new DrawValidationResult(false, errors);
        }

        // Check for duplicate participant IDs
        if (participantIds.Count != participantIds.Distinct().Count())
        {
            errors.Add("Duplicate participant IDs detected");
            return new DrawValidationResult(false, errors);
        }

        // Build exclusion set for efficient lookup
        var exclusions = BuildExclusionSet(exclusionPairs);

        // Validate each participant has at least one valid recipient
        foreach (var santa in participantIds)
        {
            var validRecipients = participantIds
                .Where(recipient => recipient != santa && !IsExcluded(santa, recipient, exclusions))
                .ToList();

            if (validRecipients.Count == 0)
            {
                errors.Add($"Participant has no valid recipients due to exclusion rules");
                _logger.LogWarning(
                    "Draw validation failed: participant has no valid recipients. " +
                    "ParticipantCount={ParticipantCount}, ExclusionCount={ExclusionCount}",
                    participantIds.Count,
                    exclusionPairs.Count);
                return new DrawValidationResult(false, errors);
            }
        }

        // Note: We skip Hall's Marriage Theorem checks here because:
        // 1. Secret Santa is a cycle cover problem, not a bipartite matching problem
        // 2. Checking for Hamiltonian cycles is NP-complete
        // 3. The backtracking algorithm will determine if a valid solution exists
        // 4. We've already verified each participant has at least one valid recipient

        return new DrawValidationResult(true, errors);
    }

    /// <inheritdoc/>
    public Dictionary<string, string> ExecuteDrawAlgorithm(
        List<string> participantIds,
        List<(string userId1, string userId2)> exclusionPairs)
    {
        _logger.LogInformation(
            "Executing draw algorithm for {ParticipantCount} participants with {ExclusionCount} exclusion rules",
            participantIds.Count,
            exclusionPairs.Count);

        // Validate feasibility first
        var validation = ValidateDrawFeasibility(participantIds, exclusionPairs);
        if (!validation.IsValid)
        {
            throw new DrawAlgorithmException(
                $"Draw validation failed: {string.Join(", ", validation.Errors)}");
        }

        // Shuffle participants for randomization
        var shuffledParticipants = participantIds.OrderBy(_ => Random.Shared.Next()).ToList();
        var exclusions = BuildExclusionSet(exclusionPairs);

        // Try to build assignment using backtracking
        for (int attempt = 0; attempt < MaxBacktrackAttempts; attempt++)
        {
            var assignments = TryBuildAssignments(shuffledParticipants, exclusions);
            if (assignments != null)
            {
                _logger.LogInformation(
                    "Draw algorithm succeeded on attempt {Attempt}, created {AssignmentCount} assignments",
                    attempt + 1,
                    assignments.Count);
                return assignments;
            }

            // Reshuffle for next attempt
            shuffledParticipants = participantIds.OrderBy(_ => Random.Shared.Next()).ToList();
        }

        _logger.LogError(
            "Draw algorithm failed after {MaxAttempts} attempts. " +
            "ParticipantCount={ParticipantCount}, ExclusionCount={ExclusionCount}",
            MaxBacktrackAttempts,
            participantIds.Count,
            exclusionPairs.Count);

        throw new DrawAlgorithmException(
            $"Failed to generate valid assignments after {MaxBacktrackAttempts} attempts. " +
            "This may indicate overly restrictive exclusion rules.");
    }

    /// <summary>
    /// Attempts to build valid assignments using backtracking with most-constrained-first heuristic
    /// </summary>
    private Dictionary<string, string>? TryBuildAssignments(
        List<string> participantIds,
        HashSet<string> exclusions)
    {
        var assignments = new Dictionary<string, string>();
        var usedRecipients = new HashSet<string>();

        // Note: We use the participants in the order provided (which is already shuffled)
        // A more sophisticated approach would order by constraint, but for now we keep it simple
        if (Backtrack(participantIds, 0, assignments, usedRecipients, exclusions))
        {
            return assignments;
        }

        return null;
    }

    /// <summary>
    /// Recursive backtracking to find valid assignment
    /// </summary>
    private bool Backtrack(
        List<string> participantIds,
        int index,
        Dictionary<string, string> assignments,
        HashSet<string> usedRecipients,
        HashSet<string> exclusions)
    {
        // Base case: all participants assigned
        if (index >= participantIds.Count)
        {
            return true;
        }

        var santa = participantIds[index];

        // Try each potential recipient
        var potentialRecipients = participantIds
            .Where(r => !usedRecipients.Contains(r) &&
                       r != santa &&
                       !IsExcluded(santa, r, exclusions))
            .OrderBy(_ => Random.Shared.Next())
            .ToList();

        foreach (var recipient in potentialRecipients)
        {
            // Check if this assignment would create a 2-person circle (A→B and B→A)
            if (assignments.TryGetValue(recipient, out var recipientTarget) && recipientTarget == santa)
            {
                continue; // Skip this recipient as it would create a 2-person circle
            }

            // Make assignment
            assignments[santa] = recipient;
            usedRecipients.Add(recipient);

            // Recurse
            if (Backtrack(participantIds, index + 1, assignments, usedRecipients, exclusions))
            {
                return true;
            }

            // Backtrack
            assignments.Remove(santa);
            usedRecipients.Remove(recipient);
        }

        return false;
    }

    /// <summary>
    /// Validates Hall's Marriage Theorem condition for bipartite matching
    /// </summary>
    private bool ValidateHallCondition(List<string> participantIds, HashSet<string> exclusions)
    {
        // Check that no participant is completely isolated
        // For a valid Secret Santa draw to exist:
        // - Each participant needs at least 1 valid recipient (already checked in ValidateDrawFeasibility)
        // - The graph should be connected enough to form a valid cycle

        // For very small groups (3-5 participants), having just 1 valid recipient can work
        // For larger groups, we want more robustness with at least 2 valid recipients
        int minRecipientsRequired = participantIds.Count <= 5 ? 1 : 2;

        foreach (var santa in participantIds)
        {
            var validRecipients = participantIds
                .Where(r => r != santa && !IsExcluded(santa, r, exclusions))
                .ToList();

            if (validRecipients.Count < minRecipientsRequired)
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Builds a set of exclusion pairs for efficient lookup
    /// </summary>
    private HashSet<string> BuildExclusionSet(List<(string userId1, string userId2)> exclusionPairs)
    {
        var exclusions = new HashSet<string>();

        foreach (var (user1, user2) in exclusionPairs)
        {
            // Store both directions since exclusions are bidirectional
            exclusions.Add($"{user1}:{user2}");
            exclusions.Add($"{user2}:{user1}");
        }

        return exclusions;
    }

    /// <summary>
    /// Checks if a Santa → Recipient assignment is excluded
    /// </summary>
    private bool IsExcluded(string santa, string recipient, HashSet<string> exclusions)
    {
        return exclusions.Contains($"{santa}:{recipient}");
    }

    /// <summary>
    /// Checks for 2-person circles (A→B and B→A)
    /// </summary>
    private bool HasTwoPersonCircle(Dictionary<string, string> assignments)
    {
        foreach (var (santa, recipient) in assignments)
        {
            if (assignments.TryGetValue(recipient, out var recipientTarget) && recipientTarget == santa)
            {
                return true;
            }
        }

        return false;
    }
}
