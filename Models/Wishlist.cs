﻿using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Wishlist
{
    public int WishlistId { get; set; }

    public int? UserId { get; set; }

    public int? ProductId { get; set; }

    public DateTime? AddedDate { get; set; }

    public virtual Product? Product { get; set; }

    public virtual User? User { get; set; }
}
