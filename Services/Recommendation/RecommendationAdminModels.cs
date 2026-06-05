using System.Text.Json.Serialization;

namespace Tech_Store.Services.Recommendation;

public sealed class RecommendationAdminOperationResult
{
    public bool Success { get; init; }

    public bool IsDisabled { get; init; }

    public int? StatusCode { get; init; }

    public string Message { get; init; } = string.Empty;

    public string? ResponseBody { get; init; }
}

public sealed class SyncProductRecommendationRequest
{
    [JsonPropertyName("product_sys_id")]
    public string ProductSysId { get; init; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; init; } = "upsert";
}
