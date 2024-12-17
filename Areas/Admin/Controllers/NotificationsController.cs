using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class NotificationsController : BaseAdminController
    {
        public NotificationsController(ApplicationDbContext context) : base(context) { }

        [HttpGet("")]
        public async Task<IActionResult> GetUserNotifications()
        {
            int userId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            var notifications = await _context.UserNotifications
                .Where(un => un.UserId == userId)
                .Include(un => un.Notification)
                .OrderByDescending(un => un.Notification.CreatedAt)
                .Select(un => new
                {
                    un.UserId,
                    un.Notification.Message,
                    un.Notification.RedirectUrl,
                    un.Notification.Type,
                    un.Notification.CreatedAt,
                    un.IsRead
                })
                .ToListAsync();

            return Ok(notifications);
        }


        [HttpPost("mark-all-as-read/{userId}")]
        public async Task<IActionResult> MarkAllAsRead(string userId)
        {
            int id_user = int.Parse(userId);
            var userNotifications = await _context.UserNotifications
                .Where(un => un.UserId == id_user && (un.IsRead == null || un.IsRead == false))
                .ToListAsync();

            foreach (var un in userNotifications)
            {
                un.IsRead = true;
                un.ReadAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }
    }
}
