namespace Tech_Store.Services.Payment
{
    public class SePayOptions
    {
        public string Environment { get; set; } = "sandbox";

        public string MerchantId { get; set; } = string.Empty;

        public string SecretKey { get; set; } = string.Empty;

        public string PublicBaseUrl { get; set; } = string.Empty;

        public string SandboxCheckoutUrl { get; set; } = "https://sandbox-gateway.sepay.vn/checkout";

        public string ProductionCheckoutUrl { get; set; } = "https://gateway.sepay.vn/checkout";

        public string IpnPath { get; set; } = "/Payment/SePayIpn";
    }
}
