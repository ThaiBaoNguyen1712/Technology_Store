﻿using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class CartItem
{
    public int CartItemId { get; set; }

    public int? CartId { get; set; }

    public int ProductId { get; set; }

    public int VarientProductId { get; set; }

    public int Quantity { get; set; }

    public virtual Cart? Cart { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual VarientProduct VarientProduct { get; set; } = null!;
}
