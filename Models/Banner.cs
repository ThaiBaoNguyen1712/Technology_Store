using Org.BouncyCastle.Bcpg.Sig;

namespace Tech_Store.Models
{
    public class Banner
    {
        public int BannerId { get; set; }
        public string? ImageUrl { get; set; } 
        public string? LinkUrl { get; set; } 
        public string? Type { get; set; }
        public string? RefId { get; set; }
        public string? Position { get; set; }
        public string? Device {  get; set; }
        public int? SortOrder { get; set; }
        public bool? isActive {  get; set; }
    }
}
