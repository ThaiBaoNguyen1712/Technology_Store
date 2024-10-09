using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class VarientProduct
{
    public int VarientId { get; set; }

    public int ProductId { get; set; }

    public string Attributes { get; set; } = null!;

    public decimal Price { get; set; }
    public string Sku { get; set; } = null!;

    public int? Stock { get; set; }

    public DateTime? CreatedAt { get; set; }
}
