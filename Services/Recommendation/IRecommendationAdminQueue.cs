namespace Tech_Store.Services.Recommendation;

public interface IRecommendationAdminQueue
{
    ValueTask QueueSyncProductAsync(
        string productSysId,
        string action,
        CancellationToken cancellationToken = default);

    ValueTask QueueRebuildIndexAsync(CancellationToken cancellationToken = default);
}
