using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;
using Tech_Store.Models;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ApplicationDbContext _context;
    public AuthMiddleware(RequestDelegate next,ApplicationDbContext context)
    {
        _next = next;
        _context = context;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        // Lấy thông tin từ cookie
        var userEmail = context.Request.Cookies["UserEmail"];
        var userToken = context.Request.Cookies["UserToken"];

        // Nếu cookie không tồn tại, người dùng chưa được xác thực
        if (string.IsNullOrEmpty(userEmail) || string.IsNullOrEmpty(userToken))
        {
            // Chuyển hướng đến trang đăng nhập
            context.Response.Redirect("/Account/Login"); // Thay đổi đường dẫn nếu cần
            return;
        }

        var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == userEmail);
        if (user == null || !user.Roles.Any(r => r.RoleName == "Admin"))
        {
            // Chuyển hướng đến trang đăng nhập nếu người dùng không tồn tại hoặc không có vai trò Admin
            context.Response.Redirect("/Account/Login"); // Thay đổi đường dẫn nếu cần
            return;
        }

        // Nếu bạn có thêm logic kiểm tra token, hãy thực hiện ở đây
        // Ví dụ: xác minh token trong cơ sở dữ liệu hoặc kiểm tra thời gian hết hạn

        // Nếu mọi thứ hợp lệ, tiếp tục xử lý yêu cầu
        await _next(context);
    }
}
