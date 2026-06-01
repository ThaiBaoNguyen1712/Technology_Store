namespace Tech_Store.Models.ViewModel
{
    public class AdminRightDrawerViewModel
    {
        public string DrawerId { get; set; } = "adminRightDrawer";

        public string Title { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? Subtitle { get; set; }

        public string BodyId { get; set; } = "adminRightDrawerBody";

        public string WidthClass { get; set; } = "admin-right-drawer--lg";
    }
}
