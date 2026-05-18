using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.Enums;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Services.Payment
{
    public class PaymentGatewaySettingsService : IPaymentGatewaySettingsService
    {
        private sealed record GatewayDefinition(
            PaymentMethodType PaymentMethod,
            string SettingKey,
            string DisplayName,
            string LogoUrl,
            string ChannelLabel,
            bool DefaultEnabled);

        private static readonly GatewayDefinition[] GatewayDefinitions =
        {
            new(
                PaymentMethodType.Momo,
                PaymentGatewaySettingKeys.MomoEnabled,
                "Ví MoMo",
                "/Upload/Logo/LogoMoMo.webp",
                "Ví điện tử",
                true),
            new(
                PaymentMethodType.VnPay,
                PaymentGatewaySettingKeys.VnPayEnabled,
                "VNPay",
                "/Upload/Logo/LogoVNPay.png",
                "Cổng thanh toán",
                true),
            new(
                PaymentMethodType.SePay,
                PaymentGatewaySettingKeys.SePayEnabled,
                "SePay",
                "/Upload/Logo/LogoSePay.svg",
                "Chuyển khoản tự động",
                false)
        };

        private readonly ApplicationDbContext _context;

        public PaymentGatewaySettingsService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<PaymentGatewaySettingItemViewModel>> GetGatewaySettingsAsync(CancellationToken cancellationToken = default)
        {
            var settings = await EnsureGatewaySettingsAsync(cancellationToken);

            return GatewayDefinitions
                .Select(definition =>
                {
                    var value = settings.First(x => x.Key == definition.SettingKey).Value;
                    return new PaymentGatewaySettingItemViewModel
                    {
                        Code = definition.PaymentMethod.ToCode(),
                        DisplayName = definition.DisplayName,
                        Description = definition.DisplayName,
                        LogoUrl = definition.LogoUrl,
                        ChannelLabel = definition.ChannelLabel,
                        IsEnabled = ParseBoolean(value, definition.DefaultEnabled)
                    };
                })
                .ToList();
        }

        public async Task<bool> IsGatewayEnabledAsync(string paymentMethodCode, CancellationToken cancellationToken = default)
        {
            if (!PaymentMethodTypeExtensions.TryParseCode(paymentMethodCode, out var paymentMethodType))
            {
                return false;
            }

            var gateway = GatewayDefinitions.FirstOrDefault(x => x.PaymentMethod == paymentMethodType);
            if (gateway == null)
            {
                return false;
            }

            var settings = await EnsureGatewaySettingsAsync(cancellationToken);
            var setting = settings.First(x => x.Key == gateway.SettingKey);
            return ParseBoolean(setting.Value, gateway.DefaultEnabled);
        }

        public async Task UpdateGatewayStatusesAsync(
            bool isMomoEnabled,
            bool isVnPayEnabled,
            bool isSePayEnabled,
            CancellationToken cancellationToken = default)
        {
            var settings = await EnsureGatewaySettingsAsync(cancellationToken);

            UpdateSettingValue(settings, PaymentGatewaySettingKeys.MomoEnabled, isMomoEnabled);
            UpdateSettingValue(settings, PaymentGatewaySettingKeys.VnPayEnabled, isVnPayEnabled);
            UpdateSettingValue(settings, PaymentGatewaySettingKeys.SePayEnabled, isSePayEnabled);

            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<List<Setting>> EnsureGatewaySettingsAsync(CancellationToken cancellationToken)
        {
            var paymentSettings = await _context.Settings
                .Where(x => x.Group == "Payment")
                .ToListAsync(cancellationToken);

            var changed = false;

            foreach (var definition in GatewayDefinitions)
            {
                if (paymentSettings.Any(x => x.Key == definition.SettingKey))
                {
                    continue;
                }

                var newSetting = new Setting
                {
                    Key = definition.SettingKey,
                    Group = "Payment",
                    Value = definition.DefaultEnabled ? bool.TrueString.ToLowerInvariant() : bool.FalseString.ToLowerInvariant(),
                    DataType = "bool",
                    Description = definition.DisplayName
                };

                _context.Settings.Add(newSetting);
                paymentSettings.Add(newSetting);
                changed = true;
            }

            if (changed)
            {
                await _context.SaveChangesAsync(cancellationToken);
            }

            return paymentSettings;
        }

        private static void UpdateSettingValue(IEnumerable<Setting> settings, string key, bool value)
        {
            var setting = settings.First(x => x.Key == key);
            setting.Value = value ? bool.TrueString.ToLowerInvariant() : bool.FalseString.ToLowerInvariant();
        }

        private static bool ParseBoolean(string? value, bool fallback)
        {
            return bool.TryParse(value, out var parsedValue) ? parsedValue : fallback;
        }
    }
}
