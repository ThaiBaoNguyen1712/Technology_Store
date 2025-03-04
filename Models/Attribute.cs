using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Attribute
{
    public int AttributeId { get; set; }

    public string Code { get; set; } = null!;

    public string Name { get; set; } = null!;

    public int SortOrder { get; set; }

    public bool IsActive { get; set; }

    public virtual ICollection<AttributeValue> AttributeValues { get; set; } = new List<AttributeValue>();
}
