using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Models;

namespace Tech_Store.Controllers
{
    [Route("Notifications")]
    public class NotificationsController : BaseController
    {
        public NotificationsController(ApplicationDbContext context) : base(context) { }
        
        [HttpGet("GetUserNotifications")]
        public async Task<IActionResult> GetUserNotifications()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notifications = await _context.UserNotifications
                .Where(un => un.UserId == userId)
                .Include(un => un.Notification).ThenInclude(us=>us.UserNotifications)
                .OrderByDescending(un => un.Notification.CreatedAt)
                .Select(un => new
                {
                    un.UserId,
                    un.UserNotificationId,
                    un.Notification.Title,
                    un.Notification.Message,
                    un.Notification.RedirectUrl,
                    un.Notification.Type,
                    un.Notification.CreatedAt,
                    un.IsRead
                })
                .ToListAsync();

            return Ok(notifications);
        }


        [HttpPost("mark-all-as-read")]
        public async Task<IActionResult> MarkAllAsRead()
        {
            int id_user = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var userNotifications = await _context.UserNotifications
                .Where(un => un.UserId == id_user && (un.IsRead == null || un.IsRead == false))
                .ToListAsync();

            foreach (var un in userNotifications)
            {
                un.IsRead = true;
                un.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
        [HttpPost("mark-as-read")]
        public async Task<IActionResult> MarkAsRead(int id)
        {
            try
            {
                int id_user = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
                //Đánh dấu đã đọc chỉ cho những thông báo chưa được đọc
                var notification = await _context.UserNotifications.FirstOrDefaultAsync(un => un.UserNotificationId == id
                && (un.IsRead == null || un.IsRead == false));

                if (notification == null)
                {
                    return Json(new { success = false, message = "Thông báo không tồn tại hoặc đã được đọc." });
                }

                notification.IsRead = true;
                notification.ReadAt = DateTime.Now;

                await _context.SaveChangesAsync();
                return Json(new { success = true, isRead = notification.IsRead });
            }
            catch (Exception ex)
            {
                // Ghi log lỗi tại đây
                return Json(new { success = false, message = "Đã xảy ra lỗi khi đánh dấu thông báo.", error = ex.Message });
            }
        }

    }
}
