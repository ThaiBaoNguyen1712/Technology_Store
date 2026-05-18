namespace Tech_Store.Models.DTO.Payment.Admin
{
    public class PaymentGatewaySettingsUpdateDTo
    {
        public bool IsMomoEnabled { get; set; }

        public bool IsVnPayEnabled { get; set; }

        public bool IsSePayEnabled { get; set; }
    }
}
