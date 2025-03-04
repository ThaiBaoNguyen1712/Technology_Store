using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PusherServer;
using Tech_Store.Models;
using Tech_Store.Models.DTO.Chat;
namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    [Authorize(Roles = "Admin")]
    public class ChatsController : BaseAdminController
    {
        private readonly PusherOptions _options;
        public ChatsController(ApplicationDbContext context) : base(context)
        {
            _options = new PusherOptions
            {
                Cluster = "ap1", // Thay bằng cluster của bạn
            };
        }
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            return View();
        }
        [HttpPost("send-message")]
        public async Task<IActionResult> SendMessage([FromBody] ChatMessage message)
        {
            var pusher = new Pusher(
                "1919870",  // App ID từ Pusher
                "e04ebb5415d65ba220ed",     // Key từ Pusher
                "09e32170e2928b791eb2",  // Secret từ Pusher
                _options
            );

            // Gửi tin nhắn đến Pusher
            await pusher.TriggerAsync(
                "chat-channel", // Channel mà tin nhắn sẽ được gửi
                "new-message",  // Sự kiện để lắng nghe
                message         // Dữ liệu tin nhắn
            );

            return Ok(new { status = "Message Sent" });
        }

    }
}
