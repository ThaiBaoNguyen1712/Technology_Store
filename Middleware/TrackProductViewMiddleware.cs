using System.Security.Claims;
using Tech_Store.Services;

namespace Tech_Store.Middleware
{
    public class TrackProductViewMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IServiceScopeFactory _scopeFactory;

        public TrackProductViewMiddleware(RequestDelegate next, IServiceScopeFactory scopeFactory)
        {
            _next = next;
            _scopeFactory = scopeFactory;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // --- PHẦN 1: XỬ LÝ TRƯỚC KHI VÀO CONTROLLER ---
            // Kiểm tra nhanh điều kiện Path để tránh xử lý thừa cho mọi request
            bool isViewProduct = context.Request.Path.StartsWithSegments("/View", StringComparison.OrdinalIgnoreCase)
                                 && context.Request.Method == HttpMethods.Get;

            string guestId = null;
            if (isViewProduct)
            {
                // Đảm bảo có guest_id ngay từ đầu để dùng cho cả Controller và Task ngầm
                if (!context.Request.Cookies.TryGetValue("guest_id", out guestId))
                {
                    guestId = Guid.NewGuid().ToString();
                    context.Response.Cookies.Append("guest_id", guestId, new CookieOptions
                    {
                        Expires = DateTimeOffset.UtcNow.AddDays(7),
                        HttpOnly = true,
                        Path = "/" // Đảm bảo cookie có hiệu lực toàn trang
                    });
                }
            }

            // Cho request đi tiếp vào Controller để Render View ngay lập tức
            await _next(context);

            // --- PHẦN 2: XỬ LÝ SAU KHI CONTROLLER CHẠY XONG (CHẠY NGẦM) ---
            if (isViewProduct && context.Items.TryGetValue("product_sys_id", out var sysIdObj))
            {
                var productSysId = sysIdObj?.ToString();
                if (string.IsNullOrWhiteSpace(productSysId)) return;

                // Trích xuất thông tin cần thiết trước khi đẩy vào luồng ngầm
                var userIdStr = context.User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Fire-and-forget: Bắn task chạy ngầm và kết thúc request chính ngay tại đây
                _ = Task.Run(async () =>
                {
                    using var scope = _scopeFactory.CreateScope();
                    var redisService = scope.ServiceProvider.GetRequiredService<RedisService>();
                    var logger = scope.ServiceProvider.GetRequiredService<ILogger<TrackProductViewMiddleware>>();

                    try
                    {
                        if (int.TryParse(userIdStr, out var userId))
                        {
                            await redisService.TrackUserWatchedProductAsync(userId, productSysId);
                        }
                        else if (!string.IsNullOrEmpty(guestId))
                        {
                            await redisService.TrackGuestWatchedProductAsync(guestId, productSysId);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Background Tracking Error for Product: {ProductSysId}", productSysId);
                    }
                });
            }
        }
    }
}