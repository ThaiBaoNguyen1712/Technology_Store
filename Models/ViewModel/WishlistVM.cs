namespace Tech_Store.Models.ViewModel
{
    public class WishlistVM
    {
        public int ProductId { get; set; }
        public string Slug { get; set; }
        public string ProductName { get; set; }
        public string ImageUrl { get; set; }
        public string Price { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
