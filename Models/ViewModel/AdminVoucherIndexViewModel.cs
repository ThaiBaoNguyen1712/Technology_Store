namespace Tech_Store.Models.ViewModel;

public class AdminVoucherIndexViewModel
{
    public string? Code { get; set; }

    public string? Name { get; set; }

    public DateTime? DateFrom { get; set; }

    public DateTime? DateTo { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 20;

    public int TotalItems { get; set; }

    public int TotalPages { get; set; } = 1;

    public List<AdminVoucherIndexItemViewModel> Vouchers { get; set; } = new();
}
