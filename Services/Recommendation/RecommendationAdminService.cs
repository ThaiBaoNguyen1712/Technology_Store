using System.Net.Http.Json;
using System.Text.Json;

namespace Tech_Store.Services.Recommendation;

public sealed class RecommendationAdminService : IRecommendationAdminService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _httpClient;
    private readonly ILogger<RecommendationAdminService> _logger;
    private readonly bool _enabled;

    public RecommendationAdminService(HttpClient httpClient, IConfiguration configuration, ILogger<RecommendationAdminService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var baseUrl = configuration["RecommendApi:BaseUrl"];
        _enabled = !string.IsNullOrWhiteSpace(baseUrl);

        if (_enabled)
        {
            _httpClient.BaseAddress = new Uri(baseUrl!);
            _httpClient.Timeout = TimeSpan.FromSeconds(15);
        }
    }

    public Task<RecommendationAdminOperationResult> CheckHealthAsync(CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Get, "health", null, cancellationToken);

    public Task<RecommendationAdminOperationResult> RebuildIndexAsync(CancellationToken cancellationToken = default)
        => SendAsync(HttpMethod.Post, "admin/rebuild-index", null, cancellationToken);

    public Task<RecommendationAdminOperationResult> SyncProductAsync(
        string productSysId,
        string action,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(productSysId))
        {
            throw new ArgumentException("ProductSysId is required.", nameof(productSysId));
        }

        if (!string.Equals(action, "upsert", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(action, "delete", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("Action must be upsert or delete.", nameof(action));
        }

        return SendAsync(
            HttpMethod.Post,
            "admin/sync-product",
            new SyncProductRecommendationRequest
            {
                ProductSysId = productSysId.Trim(),
                Action = action.Trim().ToLowerInvariant()
            },
            cancellationToken);
    }

    private async Task<RecommendationAdminOperationResult> SendAsync(
        HttpMethod method,
        string relativeUrl,
        object? payload,
        CancellationToken cancellationToken)
    {
        if (!_enabled)
        {
            return new RecommendationAdminOperationResult
            {
                IsDisabled = true,
                Message = "Recommendation API is disabled because RecommendApi:BaseUrl is not configured."
            };
        }

        try
        {
            using var request = new HttpRequestMessage(method, relativeUrl);
            if (payload is not null)
            {
                request.Content = JsonContent.Create(payload, options: JsonOptions);
            }

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return new RecommendationAdminOperationResult
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode,
                    Message = "Recommendation admin request completed successfully.",
                    ResponseBody = responseBody
                };
            }

            _logger.LogWarning(
                "Recommendation admin request to {Url} failed with status {StatusCode}. Body: {Body}",
                relativeUrl,
                (int)response.StatusCode,
                responseBody);

            return new RecommendationAdminOperationResult
            {
                StatusCode = (int)response.StatusCode,
                Message = $"Recommendation admin request failed with status {(int)response.StatusCode}.",
                ResponseBody = responseBody
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recommendation admin request to {Url} failed.", relativeUrl);
            return new RecommendationAdminOperationResult
            {
                Message = ex.Message
            };
        }
    }
}
