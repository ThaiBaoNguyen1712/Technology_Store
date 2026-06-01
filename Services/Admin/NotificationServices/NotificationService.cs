using MediatR;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Events;
using Tech_Store.Hubs;
using Tech_Store.Models;

namespace Tech_Store.Services.Admin.NotificationServices
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMediator _mediator;

        public NotificationService(ApplicationDbContext context, IMediator mediator)
        {
            _context = context;
            _mediator = mediator;
        }

        // Phương thức chung để gửi thông báo
        public async Task NotifyAsync(NotificationTarget target, string title, string message, string type, string redirectUrl, List<int>? userIds = null)
        {
            if (!await ShouldDispatchAsync(target, type, redirectUrl))
            {
                return;
            }

            List<int> targetUserIds = await GetTargetUserIdsAsync(target, userIds);

            if (targetUserIds.Any())
            {
                await _mediator.Publish(new NotificationEvent(title, message, type, redirectUrl, target, targetUserIds));
            }
        }

        // Phương thức lấy danh sách UserId theo từng loại nhóm
        public async Task<List<int>> GetTargetUserIdsAsync(NotificationTarget target, List<int>? userIds = null)
        {
            return target switch
            {
                NotificationTarget.Admins => await GetAdminUserIdsAsync(),
                NotificationTarget.AllUsers => await GetAllUserIdsAsync(),
                NotificationTarget.OnlineUsers => GetOnlineUserIds(),
                NotificationTarget.SpecificUsers => userIds ?? new List<int>(),
                NotificationTarget.CurrentUser => userIds ?? new List<int>(),
                _ => new List<int>()
            };
        }

        private async Task<List<int>> GetAdminUserIdsAsync()
        {
            return await _context.Users
                .Where(u => u.Roles.Any(r => r.RoleName == "Admin"))
                .Select(u => u.UserId)
                .ToListAsync();
        }

        private async Task<List<int>> GetAllUserIdsAsync()
        {
            return await _context.Users
                .Select(u => u.UserId)
                .ToListAsync();
        }

        private List<int> GetOnlineUserIds()
        {
            return OnlineUserTracker.GetOnlineUserIds();
        }

        private async Task<bool> ShouldDispatchAsync(NotificationTarget target, string type, string redirectUrl)
        {
            if (target != NotificationTarget.Admins ||
                !string.Equals(type, "new register", StringComparison.OrdinalIgnoreCase) ||
                string.IsNullOrWhiteSpace(redirectUrl))
            {
                return true;
            }

            var userIdToken = redirectUrl
                .TrimEnd('/')
                .Split('/', StringSplitOptions.RemoveEmptyEntries)
                .LastOrDefault();

            if (!int.TryParse(userIdToken, out var userId))
            {
                return true;
            }

            var user = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == userId)
                .Select(x => new
                {
                    x.IsActive,
                    x.IsVerified
                })
                .FirstOrDefaultAsync();

            return user?.IsActive == true && user.IsVerified == true;
        }
    }
}
