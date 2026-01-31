namespace Tech_Store.Models
{
    public class Specs
    {
        public int SpecId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
        public virtual ICollection<SpecValue> SpecValues { get; set; } = new List<SpecValue>();
    }
}
