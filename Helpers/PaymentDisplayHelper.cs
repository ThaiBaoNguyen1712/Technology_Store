namespace Tech_Store.Helpers;

public static class PaymentDisplayHelper
{
    public static string GetLabel(string? paymentMethod)
    {
        return Normalize(paymentMethod) switch
        {
            "cod" => "Thanh toán khi nhận hàng (COD)",
            "momo" => "Thanh toán qua ví MoMo",
            "vnpay" => "Thanh toán qua VNPay",
            "sepay" => "Thanh toán qua SePay",
            "cash" => "Thanh toán tiền mặt",
            "card" => "Thanh toán bằng thẻ",
            _ => string.IsNullOrWhiteSpace(paymentMethod) ? "Không xác định" : paymentMethod.Trim()
        };
    }

    public static string GetLogoUrl(string? paymentMethod)
    {
        return Normalize(paymentMethod) switch
        {
            "cod" => "/Upload/Logo/shipcod.png",
            "momo" => "/Upload/Logo/LogoMoMo.webp",
            "vnpay" => "/Upload/Logo/LogoVNPay.png",
            "sepay" => "/Upload/Logo/logo-sepay-color-in-white.webp",
            _ => string.Empty
        };
    }

    private static string Normalize(string? paymentMethod)
    {
        return (paymentMethod ?? string.Empty).Trim().ToLowerInvariant();
    }
}
