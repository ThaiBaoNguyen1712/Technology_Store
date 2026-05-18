using Tech_Store.Models.Constants;

namespace Tech_Store.Services.Recommendation;

public sealed class UserProductEventTrackingService : IUserProductEventTrackingService
{
    private readonly IUserProductEventQueue _queue;

    public UserProductEventTrackingService(IUserProductEventQueue queue)
    {
        _queue = queue;
    }

    public Task TrackAsync(UserProductEventWriteRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
        {
            throw new ArgumentException("Event type is required.", nameof(request));
        }

        if (request.ProductId is null && string.IsNullOrWhiteSpace(request.ProductSysId))
        {
            throw new ArgumentException("ProductId or ProductSysId is required.", nameof(request));
        }

        if (request.UserId is null && string.IsNullOrWhiteSpace(request.SessionId))
        {
            throw new ArgumentException("UserId or SessionId is required.", nameof(request));
        }

        var normalized = new UserProductEventWriteRequest
        {
            UserId = request.UserId,
            SessionId = string.IsNullOrWhiteSpace(request.SessionId) ? null : request.SessionId.Trim(),
            ProductId = request.ProductId,
            ProductSysId = string.IsNullOrWhiteSpace(request.ProductSysId) ? null : request.ProductSysId.Trim(),
            EventType = request.EventType.Trim(),
            Weight = request.Weight ?? UserProductEventWeights.Resolve(request.EventType),
            Source = string.IsNullOrWhiteSpace(request.Source) ? null : request.Source.Trim(),
            MetadataJson = request.MetadataJson,
            CreatedAt = request.CreatedAt
        };

        return _queue.QueueWriteAsync(normalized, cancellationToken).AsTask();
    }

    public Task MergeSessionToUserAsync(string sessionId, int userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(sessionId))
        {
            return Task.CompletedTask;
        }

        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId));
        }

        return _queue.QueueMergeSessionAsync(sessionId.Trim(), userId, cancellationToken).AsTask();
    }
}
