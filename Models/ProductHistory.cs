using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class ProductHistory
{
    public int ProductHistoryId { get; set; }

    public int ProductId { get; set; }

    public string Type { get; set; } = null!;

    public int UserId { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<ProductHistoryDetail> ProductHistoryDetails { get; set; } = new List<ProductHistoryDetail>();

    public virtual User User { get; set; } = null!;
}
