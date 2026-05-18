using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.ViewModel;

public class AdminSupplierFormViewModel
{
    public int SupplierId { get; set; }

    [Required(ErrorMessage = "Vui lòng nhập mã nhà cung cấp.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Vui lòng nhập tên nhà cung cấp.")]
    public string Name { get; set; } = string.Empty;

    public string? ContactName { get; set; }

    public string? PhoneNumber { get; set; }

    public string? Email { get; set; }

    public string? Address { get; set; }

    public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;

    public int ReturnPage { get; set; } = 1;

    public string? ReturnCode { get; set; }

    public string? ReturnName { get; set; }

    public string? ReturnPhoneNumber { get; set; }

    public string? ReturnStatus { get; set; }
}
