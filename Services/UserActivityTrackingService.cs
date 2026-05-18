using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Services;

public interface IUserActivityTrackingService
{
    Task TrackLoginAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default);

    Task TrackRequestAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default);
}

public sealed class UserActivityTrackingService : IUserActivityTrackingService
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<UserActivityTrackingService> _logger;

    public UserActivityTrackingService(
        ApplicationDbContext context,
        ILogger<UserActivityTrackingService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task TrackLoginAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user == null)
        {
            return;
        }

        var ipAddress = ResolveClientIp(httpContext);
        var device = ResolveDevice(httpContext);
        var now = DateTime.Now;

        user.LastLogin = now;
        user.LastLoginIp = ipAddress;
        user.LastLoginDevice = device;
        user.LastRequestAt = now;
        user.LastRequestIp = ipAddress;
        user.LastRequestDevice = device;

        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task TrackRequestAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);
        if (user == null)
        {
            return;
        }

        user.LastRequestAt = DateTime.Now;
        user.LastRequestIp = ResolveClientIp(httpContext);
        user.LastRequestDevice = ResolveDevice(httpContext);

        await _context.SaveChangesAsync(cancellationToken);
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

        if (userAgent.Length > 255)
        {
            userAgent = userAgent[..255];
        }

        return userAgent;
    }
}
