namespace Tech_Store.Services.Payment
{
    public class OnlinePaymentGatewayStartResult
    {
        public bool Success { get; set; }

        public int StatusCode { get; set; }

        public string? RedirectUrl { get; set; }

        public string? ErrorMessage { get; set; }
    }
}
