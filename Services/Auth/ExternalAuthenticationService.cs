using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Events;
using Tech_Store.Helper;
using Tech_Store.Models;
using Tech_Store.Models.Enums;
using Tech_Store.Services.Admin.NotificationServices;

namespace Tech_Store.Services.Auth
{
    public class ExternalAuthenticationService : IExternalAuthenticationService
    {
        private readonly ApplicationDbContext _context;
        private readonly NotificationService _notificationService;

        public ExternalAuthenticationService(
            ApplicationDbContext context,
            NotificationService notificationService)
        {
            _context = context;
            _notificationService = notificationService;
        }

        public async Task<ExternalAuthenticationResult> GetOrCreateUserAsync(ClaimsPrincipal principal, string providerName)
        {
            if (principal?.Identities == null)
            {
                return new ExternalAuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không thể xác thực tài khoản {providerName}. Vui lòng thử lại."
                };
            }

            var claims = principal.Identities.FirstOrDefault()?.Claims.ToList();
            if (claims == null || claims.Count == 0)
            {
                return new ExternalAuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không tìm thấy thông tin người dùng từ {providerName}."
                };
            }

            var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
            var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "Unknown";
            var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "Unknown";

            if (string.IsNullOrWhiteSpace(email))
            {
                return new ExternalAuthenticationResult
                {
                    IsSuccess = false,
                    ErrorMessage = $"Không tìm thấy email từ {providerName}."
                };
            }

            var user = await _context.Users
                .Include(p => p.Roles)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                user = new User
                {
                    Email = email,
                    FirstName = firstName,
                    LastName = lastName,
                    PasswordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()),
                    IsVerified = true,
                    IsActive = true,
                    Img = "none.png"
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                await SetupUserRoleAsync(user.UserId);
                await CreateCartAsync(user.UserId);
                await _notificationService.NotifyAsync(
                    NotificationTarget.Admins,
                    "Người dùng mới",
                    $"Người dùng {user.Email} vừa tạo tài khoản!",
                    "new register",
                    $"/admin/users/{user.UserId}");

                user = await _context.Users
                    .Include(x => x.Roles)
                    .FirstOrDefaultAsync(x => x.UserId == user.UserId);
            }

            return new ExternalAuthenticationResult
            {
                IsSuccess = user != null,
                ErrorMessage = user == null ? $"Không thể xử lý đăng nhập {providerName}." : null,
                User = user
            };
        }

        private async Task SetupUserRoleAsync(int userId)
        {
            var userRole = new Dictionary<string, object>
            {
                { "UserId", userId },
                { "RoleId", 2 }
            };

            _context.Set<Dictionary<string, object>>("UserRole").Add(userRole);
            await _context.SaveChangesAsync();
        }

        private async Task CreateCartAsync(int userId)
        {
            _context.Carts.Add(new Cart
            {
                UserId = userId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            });

            await _context.SaveChangesAsync();
        }
    }
}
