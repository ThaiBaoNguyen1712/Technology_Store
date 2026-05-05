namespace Tech_Store.Models.ViewModel
{
    public class AdminNotificationIndexViewModel
    {
        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalItems { get; set; }

        public int TotalPages { get; set; }

        public List<AdminNotificationIndexItemViewModel> Notifications { get; set; } = new();
    }
}
