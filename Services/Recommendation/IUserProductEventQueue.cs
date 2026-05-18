namespace Tech_Store.Services.Recommendation;

public interface IUserProductEventQueue
{
    ValueTask QueueWriteAsync(UserProductEventWriteRequest request, CancellationToken cancellationToken = default);

    ValueTask QueueMergeSessionAsync(string sessionId, int userId, CancellationToken cancellationToken = default);
}
