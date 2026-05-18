using Microsoft.Extensions.Options;

namespace Tech_Store.Services.Payment
{
    public class SePayService : ISePayService
    {
        private readonly SePayOptions _options;

        public SePayService(IOptions<SePayOptions> options)
        {
            _options = options.Value;
        }

        public Task<OnlinePaymentGatewayStartResult> CreatePaymentUrlAsync(
            OnlinePaymentGatewayRequest request,
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            var missingSettings = GetMissingSettings();
            if (missingSettings.Count > 0)
            {
                return Task.FromResult(new OnlinePaymentGatewayStartResult
                {
                    Success = false,
                    StatusCode = StatusCodes.Status500InternalServerError,
                    ErrorMessage = $"Thiếu cấu hình SePay: {string.Join(", ", missingSettings)}."
                });
            }

            return Task.FromResult(new OnlinePaymentGatewayStartResult
            {
                Success = false,
                StatusCode = StatusCodes.Status501NotImplemented,
                ErrorMessage = "SePay đã có khung cấu hình nhưng chưa hoàn tất phần ký request và tạo form checkout."
            });
        }

        public OnlinePaymentGatewayCallbackResult ValidateCallback(HttpContext httpContext)
        {
            return new OnlinePaymentGatewayCallbackResult
            {
                Success = false,
                FailureMessage = "SePay chưa được cấu hình xác thực callback thanh toán."
            };
        }

        public string GetIpnUrl()
        {
            if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                return string.Empty;
            }

            return $"{_options.PublicBaseUrl.TrimEnd('/')}{_options.IpnPath}";
        }

        private List<string> GetMissingSettings()
        {
            var missingSettings = new List<string>();

            if (string.IsNullOrWhiteSpace(_options.MerchantId))
            {
                missingSettings.Add("SePay:MerchantId");
            }

            if (string.IsNullOrWhiteSpace(_options.SecretKey))
            {
                missingSettings.Add("SePay:SecretKey");
            }

            if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                missingSettings.Add("SePay:PublicBaseUrl");
            }

            return missingSettings;
        }
    }
}
