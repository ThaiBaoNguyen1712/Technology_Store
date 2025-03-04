using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Category
{
    public int CategoryId { get; set; }

    public string Name { get; set; } = null!;

    public string? EngTitle { get; set; }

    public string? Image { get; set; }

    public string? Description { get; set; }

    public int? Visible { get; set; }

    public virtual ICollection<Brand> Brands { get; set; } = new List<Brand>();

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
