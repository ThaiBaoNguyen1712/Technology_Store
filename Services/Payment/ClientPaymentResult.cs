using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.Enums;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Payment
{
    public class CheckoutPageResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? FormattedAddress { get; init; }
        public CheckoutPageViewModel? Model { get; init; }
    }

    public class PaymentMethodValidationResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        public PaymentMethodType? PaymentMethodType { get; init; }
    }

    public class PaymentStartResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        public string? RedirectUrl { get; init; }
    }

    public class PaymentCallbackResult
    {
        public bool Success { get; init; }
        public int StatusCode { get; init; }
        public string? ErrorMessage { get; init; }
        public bool IsGatewayFailure { get; init; }
        public string? GatewayFailureMessage { get; init; }
    }

    public class VoucherCheckResult
    {
        public bool Success { get; init; }
        public string Message { get; init; } = string.Empty;
        public Voucher? Voucher { get; init; }
    }
}
