
using System;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace SantaVibe.Api.Services;

public class UserAccessor : IUserAccessor
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserAccessor(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid GetCurrentUserId()
    {
        var userId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            // This should not happen in an authorized endpoint, but as a safeguard:
            throw new InvalidOperationException("User ID not found in token.");
        }
        return Guid.Parse(userId);
    }
}
