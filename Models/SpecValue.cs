namespace Tech_Store.Models
{
    public class SpecValue
    {
        public int SpecValueId { get; set; }

        public int SpecId { get; set; }

        public int ProductId { get; set; }

        public string Value { get; set; } = string.Empty;

        public int SortOrder { get; set; }

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual Specs Specs { get; set; } = null!;

        public virtual Product Product { get; set; } = null!;
    }
}
