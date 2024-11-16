namespace Tech_Store.Models.DTO
{
    public class VariantViewModel
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ImageUrl { get; set; }
        public string ProductName { get; set; }
        public string Sku { get; set; }
        public string Attribute { get; set; }
        public decimal SellPrice { get; set; }
        public int Stock {  get; set; }
        public string CategoryName { get; set; }
    }
}
