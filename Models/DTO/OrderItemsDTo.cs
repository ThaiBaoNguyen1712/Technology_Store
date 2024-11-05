namespace Tech_Store.Models.DTO
{
    public class OrderItemsDTo
    {
        public List<int> ListProductIds { get; set; }

        public List<int> ListVarientProductIds { get; set; }

        public List<int> Quantities { get; set; }

        public List<Decimal> EachProductPrice { get; set; }
    }
}
