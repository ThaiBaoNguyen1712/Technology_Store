using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Notification
{
    public int NotificationId { get; set; }

    public string Title { get; set; } = null!;

    public string Message { get; set; } = null!;
    public string RedirectUrl { get; set; } = null!;
    public string Type { get; set; } = null!;
    public DateTime? CreatedAt { get; set; }

    public virtual ICollection<UserNotification> UserNotifications { get; set; } = new List<UserNotification>();
}
