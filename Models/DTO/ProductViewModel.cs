namespace Tech_Store.Models.DTO
{
    public class ProductViewModel
    {
        public int ProductId { get; set; }
        public string Image { get; set; }
        public string Name { get; set; }
        public string Sku { get; set; }
        public string BrandName { get; set; }
        public string CategoryName { get; set; }
        public decimal? SellPrice { get; set; }
        public int Stock { get; set; }
        public string Status { get; set; }
    }
}
