using System.Collections.Generic;
using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.DTO;

namespace Tech_Store.Models.ViewModel
{
    public class CheckoutPageViewModel
    {
        public List<CheckoutDTo> Products { get; set; } = new();

        public UserDTo Customer { get; set; } = new();

        public decimal SubtotalAmount { get; set; }

        public decimal ShippingAmount { get; set; }

        public List<PaymentGatewaySettingItemViewModel> AvailableOnlineGateways { get; set; } = new();
    }
}
