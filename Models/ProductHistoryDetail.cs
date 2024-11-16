using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class ProductHistoryDetail
{
    public int HistoryDetailId { get; set; }

    public int HistoryId { get; set; }

    public int VarientId { get; set; }

    public int Quantity { get; set; }

    public virtual ProductHistory History { get; set; } = null!;

    public virtual VarientProduct Varient { get; set; } = null!;
}
