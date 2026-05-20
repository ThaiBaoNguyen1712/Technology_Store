namespace Tech_Store.Models
{
    public class InventoryTransactionsVM
    {
        public int Id { get; set; }

        public int InventoryTransId { get; set; }

        public int UserId { get; set; }

        public int? SupplierId { get; set; }

        public Product Product { get; set; } = null!;

        public string Type { get; set; } = string.Empty;

        public string Note { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; }

        public int ProductCount { get; set; }

        public int TotalQuantity { get; set; }

        public ICollection<InventorTransactionDetailViewModel> InventoryTransactionDetail { get; set; } = new List<InventorTransactionDetailViewModel>();

        public List<InventoryTransactionProductSummaryViewModel> Products { get; set; } = new();

        public string UserName { get; set; } = string.Empty;

        public string UserRole { get; set; } = string.Empty;

        public string? SupplierName { get; set; }

        public string? SupplierCode { get; set; }
    }

    public class InventoryTransactionProductSummaryViewModel
    {
        public int InventoryTransId { get; set; }

        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string ProductSku { get; set; } = string.Empty;

        public string? ProductImageUrl { get; set; }

        public int TotalQuantity { get; set; }

        public List<InventorTransactionDetailViewModel> Details { get; set; } = new();
    }

    public class InventorTransactionDetailViewModel
    {
        public int InventoryTransId { get; set; }

        public int VarientId { get; set; }

        public string VarientSku { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public string VarientName { get; set; } = string.Empty;
    }
}
