using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace Tech_Store.Services.Payment
{
    public class SePayService : ISePayService
    {
        private const int SignatureExpirySeconds = 300;
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
            return Task.FromResult(new OnlinePaymentGatewayStartResult
            {
                Success = true,
                StatusCode = StatusCodes.Status200OK,
                RedirectUrl = $"/Payment/SePayCheckout/{request.OrderId}"
            });
        }

        public OnlinePaymentGatewayCallbackResult ValidateCallback(HttpContext httpContext, string rawBody)
        {
            return new OnlinePaymentGatewayCallbackResult { Success = true };
        }

        public string BuildPaymentContent(int orderId)
        {
            return $"{_options.PaymentCodePrefix}{orderId}";
        }

        public string BuildQrImageUrl(decimal amount, string paymentContent)
        {
            var query = new Dictionary<string, string?>
            {
                ["acc"] = GetAccountNumber(),
                ["bank"] = GetBankCode(),
                ["amount"] = decimal.ToInt64(decimal.Truncate(amount)).ToString(),
                ["des"] = paymentContent,
                ["template"] = string.IsNullOrWhiteSpace(_options.QrTemplate) ? "compact" : _options.QrTemplate
            };

            return QueryHelpers.AddQueryString("https://qr.sepay.vn/img", query);
        }

        public string GetAccountNumber() => _options.AccountNumber.Trim();

        public string GetBankCode() => _options.BankCode.Trim();

        public string GetAccountName()
        {
            return string.IsNullOrWhiteSpace(_options.AccountName)
                ? "Nguyễn Thái Bảo"
                : _options.AccountName.Trim();
        }

        public string GetIpnUrl()
        {
            if (string.IsNullOrWhiteSpace(_options.PublicBaseUrl))
            {
                return string.Empty;
            }

            return BuildAbsoluteUrl(_options.IpnPath);
        }

        private static string ComputeHmacSha256(string payload, string secretKey)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secretKey));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToHexString(hash).ToLowerInvariant();
        }

        private static bool SecureEquals(string left, string right)
        {
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(left),
                Encoding.UTF8.GetBytes(right));
        }

        private string BuildAbsoluteUrl(string path)
        {
            var normalizedBaseUrl = _options.PublicBaseUrl.TrimEnd('/');
            var normalizedPath = path.StartsWith('/') ? path : "/" + path;
            return normalizedBaseUrl + normalizedPath;
        }
    }
}
