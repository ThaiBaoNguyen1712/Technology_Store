using MediatR;
using Microsoft.AspNetCore.SignalR;
using Tech_Store.Events;
using Tech_Store.Hubs;
using Tech_Store.Models;
using Tech_Store.Services.NotificationServices;

namespace Tech_Store.EventHandlers
{
    public class NotificationEventHandler : INotificationHandler<NotificationEvent>
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly NotificationService _notificationService;

        public NotificationEventHandler(ApplicationDbContext context, IHubContext<NotificationHub> hubContext, NotificationService notificationService)
        {
            _context = context;
            _hubContext = hubContext;
            _notificationService = notificationService;
        }

        public async Task Handle(NotificationEvent notification, CancellationToken cancellationToken)
        {
            var newNotification = new Notification
            {
                Title = notification.Title,
                Message = notification.Message,
                Type = notification.Type,
                RedirectUrl = notification.RedirectUrl
            };

            _context.Notifications.Add(newNotification);
            await _context.SaveChangesAsync();

            List<int> recipientUserIds = await _notificationService.GetTargetUserIdsAsync(notification.Target, notification.UserIds);

            if (!recipientUserIds.Any()) return;

            var userNotifications = recipientUserIds.Select(userId => new UserNotification
            {
                NotificationId = newNotification.NotificationId,
                UserId = userId
            }).ToList();

            _context.UserNotifications.AddRange(userNotifications);
            await _context.SaveChangesAsync();

            foreach (var userId in recipientUserIds)
            {
                await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotification", new
                {
                    Title = notification.Title,
                    Message = notification.Message,
                    Type = notification.Type,
                    RedirectUrl = notification.RedirectUrl,
                    CreatedAt = DateTime.Now
                });
            }
        }
    }
}
