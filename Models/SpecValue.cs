namespace Tech_Store.Models
{
    public class SpecValue
    {
        public int SpecValueId { get; set; }
        public int SpecId  { get; set; }
        public int ProductId { get; set; }
        public string Value { get; set; }
        public virtual Specs Specs { get; set; }

        public virtual Product Product { get; set; }
    }
}
