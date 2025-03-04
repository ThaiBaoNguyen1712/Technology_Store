using MediatR;

namespace Tech_Store.Events
{
    public record NotificationEvent(
        string Title,
        string Message,
        string Type,
        string RedirectUrl,
        NotificationTarget Target,
        List<int>? UserIds = null
    ) : INotification
    {
        private readonly List<string> _list;

        public NotificationEvent(string title, string message, string type, string redirectUrl, List<string> list)
            : this(title, message, type, redirectUrl, NotificationTarget.SpecificUsers, null)
        {
            _list = list;
        }
    }

    public enum NotificationTarget
    {
        OnlineUsers, // Người dùng đang online
        AllUsers, // Tất cả người dùng
        Admins, // Chỉ admin
        SpecificUsers, // Danh sách người dùng cụ thể
        CurrentUser // Người đang thực hiện thao tác (ví dụ: đặt hàng)
    }
}
