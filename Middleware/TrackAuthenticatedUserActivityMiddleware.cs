using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Tech_Store.Models;
using Tech_Store.Services;

namespace Tech_Store.Middleware;

public sealed class TrackAuthenticatedUserActivityMiddleware
{
    private static readonly TimeSpan ThrottleWindow = TimeSpan.FromMinutes(5);

    private readonly RequestDelegate _next;
    private readonly IMemoryCache _memoryCache;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<TrackAuthenticatedUserActivityMiddleware> _logger;

    public TrackAuthenticatedUserActivityMiddleware(
        RequestDelegate next,
        IMemoryCache memoryCache,
        IServiceScopeFactory scopeFactory,
        ILogger<TrackAuthenticatedUserActivityMiddleware> logger)
    {
        _next = next;
        _memoryCache = memoryCache;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task Invoke(HttpContext context)
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
        var ipAddress = ResolveClientIp(context);
        var device = ResolveDevice(context);

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var user = await dbContext.Users.FirstOrDefaultAsync(x => x.UserId == userId);
                if (user == null)
                {
                    return;
                }

                user.LastRequestAt = DateTime.Now;
                user.LastRequestIp = ipAddress;
                user.LastRequestDevice = device;
                await dbContext.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to track user activity for user {UserId}.", userId);
            }
        });
    }

    private static string ResolveClientIp(HttpContext httpContext)
    {
        var forwardedFor = httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
        {
            return forwardedFor.Split(',')[0].Trim();
        }

        var realIp = httpContext.Request.Headers["X-Real-IP"].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(realIp))
        {
            return realIp.Trim();
        }

        return httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    }

    private static string ResolveDevice(HttpContext httpContext)
    {
        var userAgent = httpContext.Request.Headers.UserAgent.ToString();
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return "unknown";
        }

        return userAgent.Length > 255 ? userAgent[..255] : userAgent;
    }
}
