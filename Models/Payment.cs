using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Payment
{
    public int PaymentId { get; set; }

    public int? OrderId { get; set; }

    public DateTime? PaymentDate { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public decimal? DiscountAmount { get; set; }

    public decimal? DeductAmount { get; set; }

    public decimal? OriginAmount { get; set; }

    public decimal Amount { get; set; }

    public string Status { get; set; } = null!;

    public virtual Order? Order { get; set; }
}
