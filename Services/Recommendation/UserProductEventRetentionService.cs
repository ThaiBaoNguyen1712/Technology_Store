using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Services.Recommendation;

public sealed class UserProductEventRetentionService : BackgroundService
{
    private const int BatchSize = 1000;
    private static readonly TimeSpan CleanupInterval = TimeSpan.FromHours(12);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserProductEventRetentionService> _logger;

    public UserProductEventRetentionService(
        IServiceScopeFactory scopeFactory,
        ILogger<UserProductEventRetentionService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunCleanupAsync(stoppingToken);

        using var timer = new PeriodicTimer(CleanupInterval);
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            await RunCleanupAsync(stoppingToken);
        }
    }

    private async Task RunCleanupAsync(CancellationToken cancellationToken)
    {
        try
        {
            var cutoff = DateTime.Now.AddDays(-UserProductEventRetentionPolicy.AggregateRetentionDays);
            var totalDeleted = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var expiredIds = await dbContext.UserProductEvents
                    .AsNoTracking()
                    .Where(x => x.LastInteractedAt < cutoff)
                    .OrderBy(x => x.Id)
                    .Select(x => x.Id)
                    .Take(BatchSize)
                    .ToListAsync(cancellationToken);

                if (expiredIds.Count == 0)
                {
                    break;
                }

                var deleted = await dbContext.UserProductEvents
                    .Where(x => expiredIds.Contains(x.Id))
                    .ExecuteDeleteAsync(cancellationToken);

                totalDeleted += deleted;

                if (deleted < BatchSize)
                {
                    break;
                }
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation(
                    "Deleted {DeletedCount} aggregated user product rows older than {RetentionDays} days.",
                    totalDeleted,
                    UserProductEventRetentionPolicy.AggregateRetentionDays);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enforce retention for aggregated user product rows.");
        }
    }
}
