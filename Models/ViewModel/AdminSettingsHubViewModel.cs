namespace Tech_Store.Models.ViewModel
{
    public class AdminSettingsHubViewModel
    {
        public string WebsiteName { get; set; } = string.Empty;

        public string SupportEmail { get; set; } = string.Empty;

        public int ProductSpecCount { get; set; }

        public int ProductAttributeCount { get; set; }
    }
}
