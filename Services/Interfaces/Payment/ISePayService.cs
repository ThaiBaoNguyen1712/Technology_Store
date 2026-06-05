namespace Tech_Store.Services.Payment
{
    public interface ISePayService
    {
        Task<OnlinePaymentGatewayStartResult> CreatePaymentUrlAsync(
            OnlinePaymentGatewayRequest request,
            HttpContext httpContext,
            CancellationToken cancellationToken = default);

        OnlinePaymentGatewayCallbackResult ValidateCallback(HttpContext httpContext, string rawBody);

        string BuildPaymentContent(int orderId);

        string BuildQrImageUrl(decimal amount, string paymentContent);

        string GetAccountNumber();

        string GetBankCode();

        string GetAccountName();

        string GetIpnUrl();
    }
}
