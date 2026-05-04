using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public int? ParentId { get; set; } 

    public string Name { get; set; } = null!;

    public string? EngTitle { get; set; }

    public string? Image { get; set; }

    public string? Description { get; set; }

    public int? Visible { get; set; }

    public int? VisibleOnCategoryPage { get; set; }

    public int? VisibleOnOtherPages { get; set; }

    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Category> ParentCategory { get; set; }
}
