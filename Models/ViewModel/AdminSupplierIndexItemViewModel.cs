namespace Tech_Store.Models.ViewModel;

public class AdminSupplierIndexItemViewModel
{
    public int SupplierId { get; set; }

    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public bool IsActive { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public int TransactionCount { get; set; }
}
