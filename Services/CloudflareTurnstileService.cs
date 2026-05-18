using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Tech_Store.Services
{
    public class CloudflareTurnstileService : ICloudflareTurnstileService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly CloudflareTurnstileOptions _options;

        public CloudflareTurnstileService(
            IHttpClientFactory httpClientFactory,
            IOptions<CloudflareTurnstileOptions> options)
        {
            _httpClientFactory = httpClientFactory;
            _options = options.Value;
        }

        public async Task<CloudflareTurnstileValidationResult> ValidateAsync(HttpRequest request, CancellationToken cancellationToken = default)
        {
            var token = request.Form[CloudflareTurnstileConstants.ResponseFieldName].ToString();
            if (string.IsNullOrWhiteSpace(token))
            {
                return CloudflareTurnstileValidationResult.Fail("Vui lòng xác nhận captcha.");
            }

            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                return CloudflareTurnstileValidationResult.Fail("Captcha chưa được cấu hình.");
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                using var formContent = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["secret"] = _options.SecretKey,
                    ["response"] = token,
                    ["remoteip"] = request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? string.Empty
                });

                using var response = await client.PostAsync(_options.VerifyEndpoint, formContent, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    return CloudflareTurnstileValidationResult.Fail("Không thể xác thực captcha.");
                }

                await using var responseStream = await response.Content.ReadAsStreamAsync(cancellationToken);
                var verifyResponse = await JsonSerializer.DeserializeAsync<TurnstileVerifyResponse>(
                    responseStream,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    },
                    cancellationToken);

                return verifyResponse?.Success == true
                    ? CloudflareTurnstileValidationResult.Success()
                    : CloudflareTurnstileValidationResult.Fail("Captcha không hợp lệ.");
            }
            catch
            {
                return CloudflareTurnstileValidationResult.Fail("Không thể xác thực captcha.");
            }
        }

        private sealed class TurnstileVerifyResponse
        {
            [JsonPropertyName("success")]
            public bool Success { get; set; }
        }
    }
}
