using System.Threading.Channels;

namespace Tech_Store.Services.Recommendation;

public sealed class RecommendationAdminBackgroundQueue : BackgroundService, IRecommendationAdminQueue
{
    private readonly Channel<IRecommendationAdminQueueCommand> _channel;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RecommendationAdminBackgroundQueue> _logger;

    public RecommendationAdminBackgroundQueue(
        IServiceScopeFactory scopeFactory,
        ILogger<RecommendationAdminBackgroundQueue> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _channel = Channel.CreateUnbounded<IRecommendationAdminQueueCommand>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    }

    public ValueTask QueueSyncProductAsync(
        string productSysId,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productSysId))
        {
            throw new ArgumentException("ProductSysId is required.", nameof(productSysId));
        }

        return _channel.Writer.WriteAsync(
            new SyncProductRecommendationCommand
            {
                ProductSysId = productSysId.Trim(),
                Action = string.IsNullOrWhiteSpace(action) ? "upsert" : action.Trim()
            },
            cancellationToken);
    }

    public ValueTask QueueRebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(new RebuildRecommendationIndexCommand(), cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var command = await _channel.Reader.ReadAsync(stoppingToken);
                await ProcessCommandAsync(command, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while processing recommendation admin queue.");
            }
        }
    }

    private async Task ProcessCommandAsync(
        IRecommendationAdminQueueCommand command,
        CancellationToken cancellationToken)
    {
        using var scope = _scopeFactory.CreateScope();
        var recommendationAdminService = scope.ServiceProvider.GetRequiredService<IRecommendationAdminService>();

        switch (command)
        {
            case SyncProductRecommendationCommand syncCommand:
                await HandleSyncProductAsync(recommendationAdminService, syncCommand, cancellationToken);
                break;
            case RebuildRecommendationIndexCommand:
                await HandleRebuildIndexAsync(recommendationAdminService, cancellationToken);
                break;
            default:
                _logger.LogWarning("Unknown recommendation admin queue command type: {CommandType}", command.GetType().Name);
                break;
        }
    }

    private async Task HandleSyncProductAsync(
        IRecommendationAdminService recommendationAdminService,
        SyncProductRecommendationCommand command,
        CancellationToken cancellationToken)
    {
        var result = await recommendationAdminService.SyncProductAsync(
            command.ProductSysId,
            command.Action,
            cancellationToken);

        if (!result.Success && !result.IsDisabled)
        {
            _logger.LogWarning(
                "Background recommendation sync failed for ProductSysId={ProductSysId}, Action={Action}. Message={Message}",
                command.ProductSysId,
                command.Action,
                result.Message);
            return;
        }

        if (result.IsDisabled)
        {
            _logger.LogInformation(
                "Recommendation sync skipped because API is disabled. ProductSysId={ProductSysId}, Action={Action}",
                command.ProductSysId,
                command.Action);
            return;
        }

        _logger.LogInformation(
            "Background recommendation sync completed for ProductSysId={ProductSysId}, Action={Action}.",
            command.ProductSysId,
            command.Action);
    }

    private async Task HandleRebuildIndexAsync(
        IRecommendationAdminService recommendationAdminService,
        CancellationToken cancellationToken)
    {
        var result = await recommendationAdminService.RebuildIndexAsync(cancellationToken);
        if (!result.Success && !result.IsDisabled)
        {
            _logger.LogWarning("Background recommendation index rebuild failed. Message={Message}", result.Message);
            return;
        }

        if (result.IsDisabled)
        {
            _logger.LogInformation("Recommendation index rebuild skipped because API is disabled.");
            return;
        }

        _logger.LogInformation("Background recommendation index rebuild completed.");
    }

    private interface IRecommendationAdminQueueCommand;

    private sealed class SyncProductRecommendationCommand : IRecommendationAdminQueueCommand
    {
        public string ProductSysId { get; init; } = string.Empty;

        public string Action { get; init; } = "upsert";
    }

    private sealed class RebuildRecommendationIndexCommand : IRecommendationAdminQueueCommand;
}
