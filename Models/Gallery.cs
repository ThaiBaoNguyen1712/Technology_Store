using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Gallery
{
    public int ImageId { get; set; }

    public string? Path { get; set; }

    public int? ProductId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Product? Product { get; set; }
}
