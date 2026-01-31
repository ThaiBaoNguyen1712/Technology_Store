namespace Tech_Store.Models
{
    public class InventoryTransactionsVM
    {
        public int Id { get; set; }
        public int InventoryTransId { get; set; }
        public Product Product { get; set; }
        public string Type { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<InventorTransactionDetailViewModel> InventoryTransactionDetail { get; set; } 
        public string UserName { get; set; }
        public string UserRole { get; set; }
    }
    public class InventorTransactionDetailViewModel
    {
        public int InventoryTransId { get; set; }
        public int VarientId { get; set; }
        public string VarientSku { get; set; }
        public int Quantity { get; set; }
        public string VarientName { get; set; }
    }

}
