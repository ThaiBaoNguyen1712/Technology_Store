namespace Tech_Store.Models.ViewModel;

public class AdminSupplierIndexViewModel
{
    public IReadOnlyList<AdminSupplierIndexItemViewModel> Suppliers { get; set; } = Array.Empty<AdminSupplierIndexItemViewModel>();

    public string? Code { get; set; }

    public string? Name { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Status { get; set; }

    public int Page { get; set; }

    public int TotalPages { get; set; }

    public int TotalItems { get; set; }

    public string QueryString { get; set; } = string.Empty;
}
