namespace Tech_Store.Services.Recommendation;

public interface IRecommendationAdminService
{
    Task<RecommendationAdminOperationResult> CheckHealthAsync(CancellationToken cancellationToken = default);

    Task<RecommendationAdminOperationResult> RebuildIndexAsync(CancellationToken cancellationToken = default);

    Task<RecommendationAdminOperationResult> SyncProductAsync(
        string productSysId,
        string action,
        CancellationToken cancellationToken = default);
}
