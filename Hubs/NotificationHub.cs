using Microsoft.AspNetCore.SignalR;

namespace Tech_Store.Hubs
{
    public class NotificationHub : Hub
    {
        public async Task SendNotification(string user, string notification)
        {
            await Clients.All.SendAsync("ReceiveNotification", user, notification);
        }
    }
}
