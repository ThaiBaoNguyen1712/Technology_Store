namespace Tech_Store.Services.Payment
{
    public class SePayOptions
    {
        public string Environment { get; set; } = "sandbox";
        public string MerchantId { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string PublicBaseUrl { get; set; } = string.Empty;
        public string AccountNumber { get; set; } = "0384064947";
        public string BankCode { get; set; } = "MBBank";
        public string AccountName { get; set; } = "NGUYEN THAI BAO";
        public string PaymentCodePrefix { get; set; } = "DH";
        public string QrTemplate { get; set; } = "compact";
        public string SandboxCheckoutUrl { get; set; } = "https://pgapi-sandbox.sepay.vn";
        public string ProductionCheckoutUrl { get; set; } = "https://pgapi.sepay.vn";
        public string FormActionUrl { get; set; } = "https://pay.sepay.vn/v1/checkout/init";
        public string IpnPath { get; set; } = "/Payment/SePayIpn";
    }
}
