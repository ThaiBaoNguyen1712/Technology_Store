using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class VariantAttribute
{
    public int VariantAttributeId { get; set; }

    public int ProductVariantId { get; set; }

    public int AttributeValueId { get; set; }

    public virtual AttributeValue AttributeValue { get; set; } = null!;

    public virtual VarientProduct ProductVariant { get; set; } = null!;
}
