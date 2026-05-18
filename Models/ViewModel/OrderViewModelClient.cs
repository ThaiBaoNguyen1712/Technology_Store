namespace Tech_Store.Models.ViewModel
{
    public class OrderViewModelClient
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public bool Is_Reviewed {  get; set; }
        public int[] variantIds { get; set; }
        public List<OrderReviewItemViewModel> ReviewItems { get; set; } = [];
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public decimal Amount { get; set; }
    }

    public class OrderReviewItemViewModel
    {
        public int ProductId { get; set; }
        public int VariantId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Attributes { get; set; } = string.Empty;
        public string ImageUrl { get; set; } = string.Empty;
    }
}
