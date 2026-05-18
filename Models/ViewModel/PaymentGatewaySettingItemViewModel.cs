namespace Tech_Store.Models.ViewModel
{
    public class PaymentGatewaySettingItemViewModel
    {
        public string Code { get; set; } = string.Empty;

        public string DisplayName { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public string LogoUrl { get; set; } = string.Empty;

        public string ChannelLabel { get; set; } = string.Empty;

        public bool IsEnabled { get; set; }
    }
}
