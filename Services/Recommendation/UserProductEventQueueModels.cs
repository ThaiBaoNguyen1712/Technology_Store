namespace Tech_Store.Services.Recommendation;

public sealed class UserProductEventWriteRequest
{
    public int? UserId { get; init; }

    public string? SessionId { get; init; }

    public int? ProductId { get; init; }

    public string? ProductSysId { get; init; }

    public string EventType { get; init; } = null!;

    public double? Weight { get; init; }

    public string? Source { get; init; }

    public string? MetadataJson { get; init; }

    public DateTime? CreatedAt { get; init; }
}

internal interface IUserProductEventQueueCommand
{
}

internal sealed class WriteUserProductEventCommand : IUserProductEventQueueCommand
{
    public required UserProductEventWriteRequest Request { get; init; }
}

internal sealed class MergeSessionUserProductEventsCommand : IUserProductEventQueueCommand
{
    public required string SessionId { get; init; }

    public required int UserId { get; init; }
}
