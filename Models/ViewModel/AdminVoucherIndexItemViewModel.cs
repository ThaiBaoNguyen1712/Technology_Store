namespace Tech_Store.Models.ViewModel;

public class AdminVoucherIndexItemViewModel
{
    public int VoucherId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public string Promotion { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public DateTime? StartedAt { get; set; }

    public DateTime? ExpiredAt { get; set; }

    public string StatusLabel { get; set; } = string.Empty;

    public string StatusTone { get; set; } = "pending";
}
