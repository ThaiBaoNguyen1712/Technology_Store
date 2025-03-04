using MediatR;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Events;
using Tech_Store.Hubs;
using Tech_Store.Models;

namespace Tech_Store.Services.NotificationServices
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
    }
}
