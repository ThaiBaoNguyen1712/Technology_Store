using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Setting
{
    public int SettingId { get; set; }

    public string Key { get; set; } = null!;

    public string Group { get; set; } = null!;

    public string? Value { get; set; }

    public string DataType { get; set; } = null!;

    public string? Description { get; set; }
}
