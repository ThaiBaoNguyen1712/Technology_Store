using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class InventoryTransactionsDetail
{
    public int InventoryTransDetailId { get; set; }

    public int InventoryTransId { get; set; }

    public int VarientId { get; set; }

    public int Quantity { get; set; }

    public virtual InventoryTransactions InventoryTransactions { get; set; } = null!;

    public virtual VarientProduct Varient { get; set; } = null!;
}
