namespace Tech_Store.Services.Recommendation;

public interface IUserProductEventTrackingService
{
    Task TrackAsync(UserProductEventWriteRequest request, CancellationToken cancellationToken = default);

    Task MergeSessionToUserAsync(string sessionId, int userId, CancellationToken cancellationToken = default);
}
