namespace Tech_Store.Models.DTO
{
    public class ListItemCartDTo
    {
       public List<CartDTo> cartDTos { get; set; }
    }
    public class CartDTo
    {
        public int VarientProductId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
    }
}
