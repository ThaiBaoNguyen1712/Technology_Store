namespace Tech_Store.Models.ViewModel
{
    public class AdminNotificationIndexItemViewModel
    {
        public int NotificationId { get; set; }

        public int UserNotificationId { get; set; }

        public string Type { get; set; } = string.Empty;

        public string TypeLabel { get; set; } = string.Empty;

        public string Title { get; set; } = string.Empty;

        public string Message { get; set; } = string.Empty;

        public string RedirectUrl { get; set; } = "#";

        public DateTime? CreatedAt { get; set; }

        public bool IsRead { get; set; }
    }
}
