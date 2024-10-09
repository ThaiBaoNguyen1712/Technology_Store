using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Brand
{
    public int BrandId { get; set; }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
