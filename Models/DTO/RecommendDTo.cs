using System.Text.Json.Serialization;

namespace Tech_Store.Models.DTO
{
    public class RecommendRawResponse
    {
        [JsonPropertyName("scene")]
        public string Scene { get; set; }

        [JsonPropertyName("recommendations")]
        public List<string> Recommendations { get; set; } = new();
    }

    public class RecommendResponse
    {
        public string Scene { get; set; }

        public List<Product> Products { get; set; } = new();
    }
}
