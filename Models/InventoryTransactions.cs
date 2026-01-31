using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models;

public partial class InventoryTransactions
{
    [Key]
    public int InventoryTransId { get; set; }

    public int ProductId { get; set; }

    public string Type { get; set; } = null!;

    public int UserId { get; set; }

    public string? Note { get; set; }

    public DateTime? CreatedAt { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual ICollection<InventoryTransactionsDetail> InventoryTransactionsDetail { get; set; } = new List<InventoryTransactionsDetail>();

    public virtual User User { get; set; } = null!;
}
