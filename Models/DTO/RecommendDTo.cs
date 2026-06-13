using System.Text.Json.Serialization;

namespace Tech_Store.Models.DTO
{
    public class RecommendationItemDto
    {
        [JsonPropertyName("product_sys_id")]
        public string ProductSysId { get; set; } = string.Empty;

        [JsonPropertyName("score")]
        public double? Score { get; set; }

        [JsonPropertyName("reason_code")]
        public string? ReasonCode { get; set; }
    }

    public class RecommendRawResponse
    {
        [JsonPropertyName("scene")]
        public string? Scene { get; set; }

        [JsonPropertyName("recommendation_items")]
        public List<RecommendationItemDto> RecommendationItems { get; set; } = new();

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();

        [JsonPropertyName("user_id")]
        public int? UserId { get; set; }

        [JsonPropertyName("product_sys_id")]
        public string? ProductSysId { get; set; }
    }

    public class RecommendResponse
    {
        public string Scene { get; set; } = string.Empty;

        public List<Product> Products { get; set; } = new();

        public string DataSource { get; set; } = "fallback";

        public bool MlAttempted { get; set; }

        public bool MlSucceeded { get; set; }

        public bool FallbackUsed { get; set; }

        public string? FallbackSource { get; set; }

        public string? MlRequestUrl { get; set; }

        public string? MlError { get; set; }

        public string? FallbackReason { get; set; }

        public int UserId { get; set; }

        public string? ProductSysId { get; set; }

        public int MlResultCount { get; set; }
    }
}
