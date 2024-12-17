using Microsoft.AspNetCore.SignalR;
using System;
using Tech_Store.Hubs;
using Tech_Store.Models;

namespace Tech_Store.Services.NotificationServices
{
    public class NotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        // Tạo thông báo và gửi đến danh sách người dùng
        public async Task CreateNotificationAsync(string title,string message,string type ,string redirectUrl, List<string> userIds)
        {
            // Tạo thông báo mới
            var notification = new Notification
            {
                Title = title,
                Message = message,
                Type = type,
                RedirectUrl = redirectUrl
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // Tạo UserNotification
            var userNotifications = userIds.Select(userId => new UserNotification
            {
                NotificationId = notification.NotificationId,
                UserId = int.Parse(userId)
            }).ToList();

            _context.UserNotifications.AddRange(userNotifications);
            await _context.SaveChangesAsync();

            // Gửi qua SignalR
            foreach (var userId in userIds)
            {
                await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
                {
                    notification.Title,
                    notification.Message,
                    notification.RedirectUrl,
                    notification.Type,
                    notification.CreatedAt
                });
            }
        }
    }
}
