using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Voucher
{
    public int VoucherId { get; set; }

    public string? Name { get; set; }

    public string? Code { get; set; }

    public string? Promotion { get; set; }

    public string? Description { get; set; }

    public int? Quantity { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }
}
