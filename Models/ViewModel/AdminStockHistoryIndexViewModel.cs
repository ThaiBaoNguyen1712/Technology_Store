using Tech_Store.Models;

namespace Tech_Store.Models.ViewModel
{
    public class AdminStockHistoryIndexViewModel
    {
        public IReadOnlyList<InventoryTransactionsVM> Items { get; set; } = Array.Empty<InventoryTransactionsVM>();

        public string? FilterCode { get; set; }

        public string? FilterType { get; set; }

        public DateOnly? StartDate { get; set; }

        public DateOnly? EndDate { get; set; }

        public int Page { get; set; }

        public int PageSize { get; set; }

        public int TotalPages { get; set; }

        public int TotalItems { get; set; }

        public int ImportCount { get; set; }

        public int ExportCount { get; set; }

        public string QueryString { get; set; } = string.Empty;
    }
}
