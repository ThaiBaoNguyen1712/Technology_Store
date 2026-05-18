using System.Collections.Generic;

namespace Tech_Store.Models.ViewModel
{
    public class AdminPaymentGatewaySettingsViewModel
    {
        public List<PaymentGatewaySettingItemViewModel> Gateways { get; set; } = new();

        public int EnabledGatewayCount { get; set; }

        public int TotalGatewayCount { get; set; }
    }
}
