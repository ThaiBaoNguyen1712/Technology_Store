using System.Security.Claims;
using Microsoft.Extensions.Caching.Memory;
using Tech_Store.Services;

namespace Tech_Store.Middleware;

public sealed class TrackAuthenticatedUserActivityMiddleware
{
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _memoryCache;

    public TrackAuthenticatedUserActivityMiddleware(RequestDelegate next, IMemoryCache memoryCache)
    {
        _next = next;
        _memoryCache = memoryCache;
    }

    public async Task Invoke(HttpContext context, IUserActivityTrackingService userActivityTrackingService)
    {
        await _next(context);

        if (context.User?.Identity?.IsAuthenticated != true)
        {
            return;
        }

        var userIdClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
        {
            return;
        }

        var throttleKey = $"user-activity:{userId}";
        if (_memoryCache.TryGetValue(throttleKey, out _))
        {
            return;
        }

        _memoryCache.Set(throttleKey, true, ThrottleWindow);
        await userActivityTrackingService.TrackRequestAsync(userId, context, context.RequestAborted);
    }
}
