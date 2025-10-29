using Microsoft.EntityFrameworkCore;
using SantaVibe.Api.Data;

namespace SantaVibe.Api.Services.DrawValidation;

/// <summary>
/// Implementation of draw validation service using graph-based validation
/// </summary>
public class DrawValidationService : IDrawValidationService
{
    private readonly ApplicationDbContext _context;

    public DrawValidationService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DrawValidationResult> ValidateDrawFeasibilityAsync(
        Guid groupId,
        CancellationToken cancellationToken = default)
    {
        var errors = new List<string>();

        // Get all participants
        var participants = await _context.GroupParticipants
            .Where(gp => gp.GroupId == groupId)
            .Select(gp => gp.UserId)
            .ToListAsync(cancellationToken);

        if (participants.Count < 3)
        {
            errors.Add("Minimum 3 participants required for draw");
            return new DrawValidationResult(false, errors);
        }

        // Get all exclusion rules
        var exclusionRules = await _context.ExclusionRules
            .Where(er => er.GroupId == groupId)
            .Select(er => new { er.UserId1, er.UserId2 })
            .ToListAsync(cancellationToken);

        // Build adjacency graph (who can be assigned to whom)
        var validAssignments = new Dictionary<string, HashSet<string>>();

        foreach (var santa in participants)
        {
            validAssignments[santa] = new HashSet<string>(participants);
            validAssignments[santa].Remove(santa); // Cannot draw self

            // Remove excluded users
            foreach (var rule in exclusionRules)
            {
                if (rule.UserId1 == santa)
                    validAssignments[santa].Remove(rule.UserId2);
                if (rule.UserId2 == santa)
                    validAssignments[santa].Remove(rule.UserId1);
            }
        }

        // Check if valid assignment exists using basic validation
        if (!HasValidAssignment(validAssignments, participants))
        {
            errors.Add("Current exclusion rules prevent valid Secret Santa assignments");
        }

        return new DrawValidationResult(errors.Count == 0, errors);
    }

    /// <summary>
    /// Validates if a valid assignment is possible
    /// Uses simplified validation: each participant must have at least one valid recipient
    /// For MVP - production should implement proper maximum bipartite matching algorithm
    /// </summary>
    private bool HasValidAssignment(
        Dictionary<string, HashSet<string>> validAssignments,
        List<string> participants)
    {
        // Basic validation: each participant must have at least one valid recipient
        foreach (var santa in participants)
        {
            if (validAssignments[santa].Count == 0)
                return false;
        }

        // Additional check: ensure there's no isolated subset that can only give to each other
        // This prevents scenarios where remaining assignments form an impossible configuration
        // For MVP, we use simplified validation
        // TODO: Implement proper maximum bipartite matching for production (e.g., Hopcroft-Karp algorithm)

        return true;
    }
}
