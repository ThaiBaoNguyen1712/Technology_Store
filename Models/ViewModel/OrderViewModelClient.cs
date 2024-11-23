namespace Tech_Store.Models.ViewModel
{
    public class OrderViewModelClient
    {
        public int Id { get; set; }
        public string ImageUrl { get; set; }
        public bool Is_Reviewed {  get; set; }
        public int[] variantIds { get; set; }
        public DateTime OrderDate { get; set; }
        public string OrderStatus { get; set; }
        public decimal Amount { get; set; }
    }
}
