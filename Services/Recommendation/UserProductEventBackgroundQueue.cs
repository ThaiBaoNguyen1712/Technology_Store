using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Services.Recommendation;

public sealed class UserProductEventBackgroundQueue : BackgroundService, IUserProductEventQueue
{
    private const int MaxBatchSize = 50;
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

                while (batch.Count < MaxBatchSize && _channel.Reader.TryRead(out var next))
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

        var writeCommands = batch.OfType<WriteUserProductEventCommand>().ToList();
        if (writeCommands.Count > 0)
        {
            await HandleWritesAsync(dbContext, writeCommands, cancellationToken);
        }

        foreach (var merge in batch.OfType<MergeSessionUserProductEventsCommand>())
        {
            await HandleMergeAsync(dbContext, merge, cancellationToken);
        }

        if (dbContext.ChangeTracker.HasChanges())
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private async Task HandleWritesAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<WriteUserProductEventCommand> commands,
        CancellationToken cancellationToken)
    {
        var resolvedProductIds = await ResolveProductIdsAsync(
            dbContext,
            commands.Select(x => x.Request).ToList(),
            cancellationToken);

        var deltas = new List<UserProductEventDelta>(commands.Count);
        foreach (var command in commands)
        {
            var request = command.Request;
            var productId = ResolveProductId(request, resolvedProductIds);
            if (!productId.HasValue || productId.Value <= 0)
            {
                _logger.LogWarning(
                    "Skip tracking event {EventType} because product could not be resolved. ProductId={ProductId}, ProductSysId={ProductSysId}",
                    request.EventType,
                    request.ProductId,
                    request.ProductSysId);
                continue;
            }

            deltas.Add(new UserProductEventDelta(
                request.UserId,
                request.SessionId,
                productId.Value,
                request.EventType,
                request.Weight ?? 0d,
                request.CreatedAt ?? DateTime.Now));
        }

        if (deltas.Count == 0)
        {
            return;
        }

        var groupedDeltas = deltas
            .GroupBy(x => new { x.UserId, x.SessionId, x.ProductId })
            .Select(group => UserProductEventAggregateDelta.Merge(group))
            .ToList();

        var userIds = groupedDeltas
            .Where(x => x.UserId.HasValue)
            .Select(x => x.UserId!.Value)
            .Distinct()
            .ToList();
        var sessionIds = groupedDeltas
            .Where(x => !string.IsNullOrWhiteSpace(x.SessionId))
            .Select(x => x.SessionId!)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        var productIds = groupedDeltas
            .Select(x => x.ProductId)
            .Distinct()
            .ToList();

        var existingRows = await dbContext.UserProductEvents
            .Where(x => productIds.Contains(x.ProductId) &&
                        ((x.UserId.HasValue && userIds.Contains(x.UserId.Value)) ||
                         (x.SessionId != null && sessionIds.Contains(x.SessionId))))
            .ToListAsync(cancellationToken);

        foreach (var delta in groupedDeltas)
        {
            var entity = existingRows.FirstOrDefault(x => IsSameAggregateKey(x, delta));
            if (entity is null)
            {
                dbContext.UserProductEvents.Add(new UserProductEvent
                {
                    UserId = delta.UserId,
                    SessionId = delta.SessionId,
                    ProductId = delta.ProductId,
                    ViewCount = Math.Max(0, delta.ViewCountDelta),
                    AddToCartCount = Math.Max(0, delta.AddToCartCountDelta),
                    WishlistCount = Math.Max(0, delta.WishlistCountDelta),
                    PurchaseCount = Math.Max(0, delta.PurchaseCountDelta),
                    InteractionScore = delta.InteractionScoreDelta,
                    LastInteractedAt = delta.LastInteractedAt
                });
                continue;
            }

            ApplyDelta(entity, delta);
        }
    }

    private static async Task<Dictionary<string, int>> ResolveProductIdsAsync(
        ApplicationDbContext dbContext,
        IReadOnlyCollection<UserProductEventWriteRequest> requests,
        CancellationToken cancellationToken)
    {
        var sysIds = requests
            .Where(x => !x.ProductId.HasValue && !string.IsNullOrWhiteSpace(x.ProductSysId))
            .Select(x => x.ProductSysId!.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (sysIds.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        }

        return await dbContext.Products
            .AsNoTracking()
            .Where(x => sysIds.Contains(x.ProductSysId))
            .ToDictionaryAsync(x => x.ProductSysId, x => x.ProductId, StringComparer.OrdinalIgnoreCase, cancellationToken);
    }

    private static int? ResolveProductId(
        UserProductEventWriteRequest request,
        IReadOnlyDictionary<string, int> resolvedProductIds)
    {
        if (request.ProductId.HasValue)
        {
            return request.ProductId.Value;
        }

        if (!string.IsNullOrWhiteSpace(request.ProductSysId) &&
            resolvedProductIds.TryGetValue(request.ProductSysId.Trim(), out var productId))
        {
            return productId;
        }

        return null;
    }

    private static async Task HandleMergeAsync(
        ApplicationDbContext dbContext,
        MergeSessionUserProductEventsCommand request,
        CancellationToken cancellationToken)
    {
        var guestRows = await dbContext.UserProductEvents
            .Where(x => x.UserId == null && x.SessionId == request.SessionId)
            .ToListAsync(cancellationToken);

        if (guestRows.Count == 0)
        {
            return;
        }

        var productIds = guestRows.Select(x => x.ProductId).Distinct().ToList();
        var userRows = await dbContext.UserProductEvents
            .Where(x => x.UserId == request.UserId && productIds.Contains(x.ProductId))
            .ToListAsync(cancellationToken);

        foreach (var guestRow in guestRows)
        {
            var userRow = userRows.FirstOrDefault(x => x.ProductId == guestRow.ProductId);
            if (userRow is null)
            {
                guestRow.UserId = request.UserId;
                guestRow.SessionId = null;
                continue;
            }

            userRow.ViewCount += guestRow.ViewCount;
            userRow.AddToCartCount += guestRow.AddToCartCount;
            userRow.WishlistCount += guestRow.WishlistCount;
            userRow.PurchaseCount += guestRow.PurchaseCount;
            userRow.InteractionScore += guestRow.InteractionScore;
            userRow.LastInteractedAt = userRow.LastInteractedAt >= guestRow.LastInteractedAt
                ? userRow.LastInteractedAt
                : guestRow.LastInteractedAt;

            dbContext.UserProductEvents.Remove(guestRow);
        }
    }

    private static bool IsSameAggregateKey(UserProductEvent entity, UserProductEventAggregateDelta delta)
    {
        if (delta.UserId.HasValue)
        {
            return entity.UserId == delta.UserId && entity.ProductId == delta.ProductId;
        }

        return entity.UserId == null &&
               entity.SessionId == delta.SessionId &&
               entity.ProductId == delta.ProductId;
    }

    private static void ApplyDelta(UserProductEvent entity, UserProductEventAggregateDelta delta)
    {
        entity.ViewCount = Math.Max(0, entity.ViewCount + delta.ViewCountDelta);
        entity.AddToCartCount = Math.Max(0, entity.AddToCartCount + delta.AddToCartCountDelta);
        entity.WishlistCount = Math.Max(0, entity.WishlistCount + delta.WishlistCountDelta);
        entity.PurchaseCount = Math.Max(0, entity.PurchaseCount + delta.PurchaseCountDelta);
        entity.InteractionScore += delta.InteractionScoreDelta;
        entity.LastInteractedAt = entity.LastInteractedAt >= delta.LastInteractedAt
            ? entity.LastInteractedAt
            : delta.LastInteractedAt;
    }

    private sealed record UserProductEventDelta(
        int? UserId,
        string? SessionId,
        int ProductId,
        string EventType,
        double Weight,
        DateTime OccurredAt);

    private sealed record UserProductEventAggregateDelta(
        int? UserId,
        string? SessionId,
        int ProductId,
        int ViewCountDelta,
        int AddToCartCountDelta,
        int WishlistCountDelta,
        int PurchaseCountDelta,
        double InteractionScoreDelta,
        DateTime LastInteractedAt)
    {
        public static UserProductEventAggregateDelta Merge(IEnumerable<UserProductEventDelta> deltas)
        {
            var aggregate = deltas.First();
            var result = new UserProductEventAggregateDelta(
                aggregate.UserId,
                aggregate.SessionId,
                aggregate.ProductId,
                0,
                0,
                0,
                0,
                0d,
                aggregate.OccurredAt);

            foreach (var delta in deltas)
            {
                result = result with
                {
                    ViewCountDelta = result.ViewCountDelta + ResolveViewDelta(delta.EventType),
                    AddToCartCountDelta = result.AddToCartCountDelta + ResolveAddToCartDelta(delta.EventType),
                    WishlistCountDelta = result.WishlistCountDelta + ResolveWishlistDelta(delta.EventType),
                    PurchaseCountDelta = result.PurchaseCountDelta + ResolvePurchaseDelta(delta.EventType),
                    InteractionScoreDelta = result.InteractionScoreDelta + delta.Weight,
                    LastInteractedAt = result.LastInteractedAt >= delta.OccurredAt ? result.LastInteractedAt : delta.OccurredAt
                };
            }

            return result;
        }

        private static int ResolveViewDelta(string eventType) =>
            eventType switch
            {
                "view_detail" => 1,
                "search_click" => 1,
                "homepage_click" => 1,
                "recommendation_click" => 1,
                _ => 0
            };

        private static int ResolveAddToCartDelta(string eventType) =>
            eventType switch
            {
                "add_cart" => 1,
                "remove_cart" => -1,
                _ => 0
            };

        private static int ResolveWishlistDelta(string eventType) =>
            eventType switch
            {
                "wishlist_add" => 1,
                "wishlist_remove" => -1,
                _ => 0
            };

        private static int ResolvePurchaseDelta(string eventType) =>
            eventType switch
            {
                "purchase" => 1,
                _ => 0
            };
    }
}
