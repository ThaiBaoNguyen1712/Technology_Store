namespace Tech_Store.Middleware
{
    public class GuestSessionMiddleware
    {
        private readonly RequestDelegate _next;
        private const string GuestCookieName = "guest_id";

        public GuestSessionMiddleware(RequestDelegate next) => _next = next;
        public async Task Invoke(HttpContext context)
        {
            if (!context.Request.Cookies.ContainsKey(GuestCookieName))
            {
                // Tạo ID mới nếu chưa có
                string guestId = Guid.NewGuid().ToString();

                context.Response.Cookies.Append(GuestCookieName, guestId, new CookieOptions
                {
                    Expires = DateTimeOffset.UtcNow.AddDays(30),
                    HttpOnly = true, 
                    SameSite = SameSiteMode.Lax
                });

                // Đưa vào Items để dùng ngay trong request hiện tại
                context.Items[GuestCookieName] = guestId;
            }

            await _next(context);
        }
    }
}
