namespace Tech_Store.Models
{
    public class Specs
    {
        public int SpecId { get; set; }

        public string Name { get; set; } = string.Empty;

        public string Code { get; set; } = string.Empty;

        public string? GroupName { get; set; }

        public string? Unit { get; set; }

        public string? Description { get; set; }

        public string InputType { get; set; } = "text";

        public int SortOrder { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFilterable { get; set; }

        public bool IsVisibleOnProductPage { get; set; } = true;

        public DateTime? CreatedAt { get; set; }

        public DateTime? UpdatedAt { get; set; }

        public virtual ICollection<SpecValue> SpecValues { get; set; } = new List<SpecValue>();
    }
}
