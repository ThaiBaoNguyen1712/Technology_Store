using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class ProductAttribute
{
    public int ProductId { get; set; }

    public int AttributeValue { get; set; }

    public virtual AttributeValue AttributeValueNavigation { get; set; } = null!;

    public virtual Product Product { get; set; } = null!;
}
