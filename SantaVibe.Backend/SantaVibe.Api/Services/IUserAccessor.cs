
using System;

namespace SantaVibe.Api.Services;

public interface IUserAccessor
{
    Guid GetCurrentUserId();
}
