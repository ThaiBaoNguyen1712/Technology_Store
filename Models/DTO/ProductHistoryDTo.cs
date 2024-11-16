namespace Tech_Store.Models.DTO
{
    public class ProductHistoryDTo
    {
        public int ProductId { get; set; }
        public List<Variant> Variants { get; set; }
        public string Type { get; set; }
        public string? Note { get; set; }
    }
    public class Variant {
        public int VariantId { get; set; }
        public int Quantity { get; set; }
    }
}
