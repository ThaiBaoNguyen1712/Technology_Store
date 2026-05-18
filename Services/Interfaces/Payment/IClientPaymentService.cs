using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.Enums;

namespace Tech_Store.Services.Payment
{
    public interface IClientPaymentService
    {
        Task<CheckoutPageResult> BuildCheckoutPageAsync(int userId, string? cartItemsJson, CancellationToken cancellationToken = default);
        Task<PaymentMethodValidationResult> ValidatePaymentMethodAsync(string? paymentMethod, CancellationToken cancellationToken = default);
        Task<PaymentStartResult> CreateOnlinePaymentAsync(PaymentMethodType paymentMethodType, PaymentDTo model, int userId, HttpContext httpContext, CancellationToken cancellationToken = default);
        Task<PaymentStartResult> CreateCodOrderAsync(PaymentDTo model, int userId, HttpContext httpContext, CancellationToken cancellationToken = default);
        Task<PaymentCallbackResult> HandleMomoCallbackAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default);
        Task<PaymentCallbackResult> HandleVnPayCallbackAsync(int userId, HttpContext httpContext, CancellationToken cancellationToken = default);
        Task<VoucherCheckResult> CheckVoucherAsync(string code, CancellationToken cancellationToken = default);
    }
}
