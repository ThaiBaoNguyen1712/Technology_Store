using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Payment
{
    public interface IPaymentGatewaySettingsService
    {
        Task<List<PaymentGatewaySettingItemViewModel>> GetGatewaySettingsAsync(CancellationToken cancellationToken = default);

        Task<bool> IsGatewayEnabledAsync(string paymentMethodCode, CancellationToken cancellationToken = default);

        Task UpdateGatewayStatusesAsync(
            bool isMomoEnabled,
            bool isVnPayEnabled,
            bool isSePayEnabled,
            CancellationToken cancellationToken = default);
    }
}
