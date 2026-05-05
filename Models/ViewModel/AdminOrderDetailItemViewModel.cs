namespace Tech_Store.Models.ViewModel
{
    public class AdminOrderDetailItemViewModel
    {
        public int ProductId { get; set; }

        public string ProductName { get; set; } = string.Empty;

        public string? ProductImage { get; set; }

        public string VariantSku { get; set; } = string.Empty;

        public string VariantDisplay { get; set; } = string.Empty;

        public int Quantity { get; set; }

        public decimal UnitPrice { get; set; }

        public decimal LineTotal { get; set; }
    }
}
