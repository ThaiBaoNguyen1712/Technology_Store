namespace Tech_Store.Models.Enums
{
    public enum PaymentMethodType
    {
        Cod = 1,
        Momo = 2,
        VnPay = 3,
        SePay = 4
    }

    public static class PaymentMethodTypeExtensions
    {
        public static bool TryParseCode(string? value, out PaymentMethodType paymentMethodType)
        {
            switch (value?.Trim().ToLowerInvariant())
            {
                case "cod":
                    paymentMethodType = PaymentMethodType.Cod;
                    return true;
                case "momo":
                    paymentMethodType = PaymentMethodType.Momo;
                    return true;
                case "vnpay":
                    paymentMethodType = PaymentMethodType.VnPay;
                    return true;
                case "sepay":
                    paymentMethodType = PaymentMethodType.SePay;
                    return true;
                default:
                    paymentMethodType = default;
                    return false;
            }
        }

        public static string ToCode(this PaymentMethodType paymentMethodType)
        {
            return paymentMethodType switch
            {
                PaymentMethodType.Cod => "cod",
                PaymentMethodType.Momo => "momo",
                PaymentMethodType.VnPay => "vnpay",
                PaymentMethodType.SePay => "sepay",
                _ => string.Empty
            };
        }

        public static string ToPaymentRecordValue(this PaymentMethodType paymentMethodType)
        {
            return paymentMethodType switch
            {
                PaymentMethodType.Cod => "COD",
                PaymentMethodType.Momo => "MoMo",
                PaymentMethodType.VnPay => "VNPay",
                PaymentMethodType.SePay => "SePay",
                _ => string.Empty
            };
        }

        public static bool IsOnlineGateway(this PaymentMethodType paymentMethodType)
        {
            return paymentMethodType == PaymentMethodType.Momo
                || paymentMethodType == PaymentMethodType.VnPay
                || paymentMethodType == PaymentMethodType.SePay;
        }
    }
}
