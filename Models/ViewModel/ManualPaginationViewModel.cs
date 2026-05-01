namespace Tech_Store.Models.ViewModel
{
    public class ManualPaginationViewModel
    {
        public int CurrentPage { get; set; }

        public int TotalPages { get; set; }

        public string BaseUrl { get; set; } = string.Empty;

        public string? QueryString { get; set; }
    }
}
