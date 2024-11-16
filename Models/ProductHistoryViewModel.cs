namespace Tech_Store.Models
{
    public class ProductHistoryViewModel
    {
        public int Id { get; set; }
        public int ProductHistoryId { get; set; }
        public Product Product { get; set; }
        public string Type { get; set; }
        public string Note { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<ProductHistoryDetailViewModel> ProductHistoryDetails { get; set; } 
        public string UserName { get; set; }
        public string UserRole { get; set; }
    }
    public class ProductHistoryDetailViewModel
    {
        public int HistoryDetailId { get; set; }
        public int VarientId { get; set; }
        public string VarientSku { get; set; }
        public int Quantity { get; set; }
        public string VarientName { get; set; }
    }

}
