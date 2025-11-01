namespace SantaVibe.Api.Features.Groups.ExecuteDraw;

/// <summary>
/// Request DTO for executing Secret Santa draw
/// </summary>
public sealed record ExecuteDrawRequest(
    decimal Budget);
