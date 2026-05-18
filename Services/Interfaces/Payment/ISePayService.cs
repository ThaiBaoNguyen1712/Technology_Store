namespace Tech_Store.Services.Payment
{
    public interface ISePayService
    {
        Task<OnlinePaymentGatewayStartResult> CreatePaymentUrlAsync(
            OnlinePaymentGatewayRequest request,
            HttpContext httpContext,
            CancellationToken cancellationToken = default);

        OnlinePaymentGatewayCallbackResult ValidateCallback(HttpContext httpContext);

        string GetIpnUrl();
    }
}
