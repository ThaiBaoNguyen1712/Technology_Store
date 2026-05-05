using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.ViewModel;

public class AdminVoucherFormViewModel
{
    public int VoucherId { get; set; }

    [Required(ErrorMessage = "Tên mã khuyến mãi là bắt buộc.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Mã khuyến mãi là bắt buộc.")]
    public string Code { get; set; } = string.Empty;

    [Required(ErrorMessage = "Giá trị khuyến mãi là bắt buộc.")]
    public decimal? PromotionValue { get; set; }

    [Required(ErrorMessage = "Loại khuyến mãi là bắt buộc.")]
    public string PromotionType { get; set; } = "amount";

    [Required(ErrorMessage = "Số lượng là bắt buộc.")]
    [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0.")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "Mô tả là bắt buộc.")]
    public string Description { get; set; } = string.Empty;

    [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc.")]
    public DateTime? StartedAt { get; set; }

    [Required(ErrorMessage = "Ngày kết thúc là bắt buộc.")]
    public DateTime? ExpiredAt { get; set; }

    public int ReturnPage { get; set; } = 1;

    public string? ReturnCode { get; set; }

    public string? ReturnName { get; set; }

    public DateTime? ReturnDateFrom { get; set; }

    public DateTime? ReturnDateTo { get; set; }
}
