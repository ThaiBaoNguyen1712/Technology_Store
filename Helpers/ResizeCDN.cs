using System.Text.RegularExpressions;

namespace Tech_Store.Helpers
{
    public static class ResizeCDN
    {
        public static string Cdn(this string imageUrl, int? w = 0, int? h = 0)
        {
            if (string.IsNullOrEmpty(imageUrl))
                return imageUrl;

            // Chuyển null về 0 để Imgproxy hiểu là "auto"
            int width = w ?? 0;
            int height = h ?? 0;

            // 1. Nếu đã là link imgproxy, thay thế thông số cũ
            if (imageUrl.Contains("/rs:fill:"))
            {
                return Regex.Replace(
                    imageUrl,
                    @"rs:fill:\d+:\d+",
                    $"rs:fill:{width}:{height}"
                );
            }

            // 2. Nếu là ảnh thường (plain url), wrap lại theo chuẩn
            // Lưu ý: Đảm bảo imageUrl truyền vào không chứa ký tự đặc biệt gây lỗi URL
            return $"https://cdn2.cellphones.com.vn/insecure/rs:fill:{width}:{height}/q:90/plain/{imageUrl}";
        }
    }
}