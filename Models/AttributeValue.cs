using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class AttributeValue
{
    public int AttributeValueId { get; set; }

    public int AttributeId { get; set; }

    public string Value { get; set; } = null!;

    public virtual Attribute Attribute { get; set; } = null!;

    public virtual ICollection<VariantAttribute> VariantAttributes { get; set; } = new List<VariantAttribute>();
}
