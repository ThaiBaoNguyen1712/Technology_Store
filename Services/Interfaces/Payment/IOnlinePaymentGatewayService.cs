using Tech_Store.Models.Enums;

namespace Tech_Store.Services.Payment
{
    public interface IOnlinePaymentGatewayService
    {
        Task<OnlinePaymentGatewayStartResult> CreatePaymentUrlAsync(
            PaymentMethodType paymentMethodType,
            OnlinePaymentGatewayRequest request,
            HttpContext httpContext,
            CancellationToken cancellationToken = default);

        OnlinePaymentGatewayCallbackResult ValidateCallback(
            PaymentMethodType paymentMethodType,
            HttpContext httpContext);
    }
}
