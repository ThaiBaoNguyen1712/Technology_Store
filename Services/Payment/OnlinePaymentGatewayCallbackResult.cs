namespace Tech_Store.Services.Payment
{
    public class OnlinePaymentGatewayCallbackResult
    {
        public bool Success { get; set; }

        public string? FailureMessage { get; set; }
    }
}
