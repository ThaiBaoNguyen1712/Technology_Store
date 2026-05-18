using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Services.Recommendation;

public sealed class UserProductEventBackgroundQueue : BackgroundService, IUserProductEventQueue
{
    private readonly Channel<IUserProductEventQueueCommand> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<UserProductEventBackgroundQueue> _logger;

    public UserProductEventBackgroundQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<UserProductEventBackgroundQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _channel = Channel.CreateUnbounded<IUserProductEventQueueCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask QueueWriteAsync(UserProductEventWriteRequest request, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(
            new WriteUserProductEventCommand { Request = request },
            cancellationToken);
    }

    public ValueTask QueueMergeSessionAsync(string sessionId, int userId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(
            new MergeSessionUserProductEventsCommand { SessionId = sessionId, UserId = userId },
            cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var command = await _channel.Reader.ReadAsync(stoppingToken);
                var batch = new List<IUserProductEventQueueCommand> { command };

                while (batch.Count < 50 && _channel.Reader.TryRead(out var next))
                {
                    batch.Add(next);
                }

                await ProcessBatchAsync(batch, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing user product event queue.");
            }
        }
    }

    private async Task ProcessBatchAsync(
        IReadOnlyCollection<IUserProductEventQueueCommand> batch,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        foreach (var command in batch)
        {
            switch (command)
            {
                case WriteUserProductEventCommand write:
                    await HandleWriteAsync(dbContext, write.Request, cancellationToken);
                    break;
                case MergeSessionUserProductEventsCommand merge:
                    await HandleMergeAsync(dbContext, merge, cancellationToken);
                    break;
            }
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandleWriteAsync(
        ApplicationDbContext dbContext,
        UserProductEventWriteRequest request,
        CancellationToken cancellationToken)
    {
        var productId = request.ProductId;

        if (!productId.HasValue && !string.IsNullOrWhiteSpace(request.ProductSysId))
        {
            productId = await dbContext.Products
                .AsNoTracking()
                .Where(x => x.ProductSysId == request.ProductSysId)
                .Select(x => (int?)x.ProductId)
                .FirstOrDefaultAsync(cancellationToken);
        }

        if (!productId.HasValue || productId.Value <= 0)
        {
            _logger.LogWarning(
                "Skip tracking event {EventType} because product could not be resolved. ProductId={ProductId}, ProductSysId={ProductSysId}",
                request.EventType,
                request.ProductId,
                request.ProductSysId);
            return;
        }

        dbContext.UserProductEvents.Add(new UserProductEvent
        {
            UserId = request.UserId,
            SessionId = request.SessionId,
            ProductId = productId.Value,
            EventType = request.EventType,
            Weight = request.Weight ?? 0d,
            Source = request.Source,
            MetadataJson = request.MetadataJson,
            CreatedAt = request.CreatedAt ?? DateTime.Now
        });
    }

    private static Task HandleMergeAsync(
        ApplicationDbContext dbContext,
        MergeSessionUserProductEventsCommand request,
        CancellationToken cancellationToken)
    {
        return dbContext.UserProductEvents
            .Where(x => x.UserId == null && x.SessionId == request.SessionId)
            .ExecuteUpdateAsync(
                updates => updates
                    .SetProperty(x => x.UserId, request.UserId)
                    .SetProperty(x => x.SessionId, (string?)null),
                cancellationToken);
    }
}
