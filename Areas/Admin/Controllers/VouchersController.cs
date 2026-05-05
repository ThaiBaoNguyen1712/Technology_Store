using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.Interfaces;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class VouchersController : BaseAdminController
    {
        private readonly IVoucherService _voucherService;
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;

        public VouchersController(
            ApplicationDbContext context,
            IVoucherService voucherService,
            IConfiguration configuration) : base(context)
        {
            _voucherService = voucherService;
            _configuration = configuration;
        }

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            var resolvedPageSize = pageSize.GetValueOrDefault(FallbackPageSize);
            return resolvedPageSize > 0 ? resolvedPageSize : FallbackPageSize;
        }

        private static IQueryable<Voucher> ApplyVoucherFilters(
            IQueryable<Voucher> query,
            string? code,
            string? name,
            DateTime? dateFrom,
            DateTime? dateTo)
        {
            if (!string.IsNullOrWhiteSpace(code))
            {
                var normalizedCode = code.Trim();
                query = query.Where(x => (x.Code ?? string.Empty).Contains(normalizedCode));
            }

            if (!string.IsNullOrWhiteSpace(name))
            {
                var normalizedName = name.Trim();
                query = query.Where(x => (x.Name ?? string.Empty).Contains(normalizedName));
            }

            if (dateFrom.HasValue)
            {
                var fromDate = dateFrom.Value.Date;
                query = query.Where(x => x.StartedAt.HasValue && x.StartedAt.Value >= fromDate);
            }

            if (dateTo.HasValue)
            {
                var toDateExclusive = dateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.ExpiredAt.HasValue && x.ExpiredAt.Value < toDateExclusive);
            }

            return query;
        }

        private static (string Label, string Tone) GetVoucherStatus(DateTime? startedAt, DateTime? expiredAt)
        {
            var now = DateTime.Now;

            if (startedAt.HasValue && startedAt.Value > now)
            {
                return ("Sắp diễn ra", "pending");
            }

            if (expiredAt.HasValue && expiredAt.Value < now)
            {
                return ("Đã hết hạn", "danger");
            }

            return ("Đang áp dụng", "success");
        }

        private static bool TryParsePromotion(string? promotion, out decimal promotionValue, out string promotionType)
        {
            promotionValue = 0;
            promotionType = "amount";

            if (string.IsNullOrWhiteSpace(promotion))
            {
                return false;
            }

            var trimmedPromotion = promotion.Trim();
            if (trimmedPromotion.EndsWith("%", StringComparison.Ordinal))
            {
                promotionType = "percent";
                trimmedPromotion = trimmedPromotion[..^1].Trim();
            }

            return decimal.TryParse(trimmedPromotion, out promotionValue);
        }

        private static string BuildPromotionDisplay(decimal? promotionValue, string? promotionType)
        {
            if (!promotionValue.HasValue)
            {
                return string.Empty;
            }

            return string.Equals(promotionType, "percent", StringComparison.OrdinalIgnoreCase)
                ? $"{promotionValue.Value:0.##}%"
                : $"{promotionValue.Value:0.##}";
        }

        private async Task<bool> ValidateVoucherFormAsync(AdminVoucherFormViewModel model)
        {
            if (model.PromotionValue.HasValue && model.PromotionValue.Value <= 0)
            {
                ModelState.AddModelError(nameof(model.PromotionValue), "Giá trị khuyến mãi phải lớn hơn 0.");
            }

            if (string.Equals(model.PromotionType, "percent", StringComparison.OrdinalIgnoreCase)
                && model.PromotionValue.HasValue
                && model.PromotionValue.Value > 100)
            {
                ModelState.AddModelError(nameof(model.PromotionValue), "Khuyến mãi theo phần trăm không được vượt quá 100.");
            }

            if (model.StartedAt.HasValue && model.ExpiredAt.HasValue && model.StartedAt.Value >= model.ExpiredAt.Value)
            {
                ModelState.AddModelError(nameof(model.ExpiredAt), "Ngày kết thúc phải sau ngày bắt đầu.");
            }

            var normalizedCode = model.Code.Trim();
            var duplicateCodeExists = model.VoucherId > 0
                ? await _context.Vouchers.AnyAsync(x => x.Code == normalizedCode && x.VoucherId != model.VoucherId)
                : await _context.Vouchers.AnyAsync(x => x.Code == normalizedCode);

            if (duplicateCodeExists)
            {
                ModelState.AddModelError(nameof(model.Code), "Mã khuyến mãi đã tồn tại.");
            }

            return ModelState.IsValid;
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(string? code, string? name, DateTime? dateFrom, DateTime? dateTo, int page = 1, int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            if (resolvedPageSize <= 0)
            {
                resolvedPageSize = GetDefaultAdminPageSize();
            }

            var query = ApplyVoucherFilters(_context.Vouchers.AsNoTracking(), code, name, dateFrom, dateTo);
            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);
            var currentPage = Math.Min(Math.Max(1, page), totalPages);

            var vouchers = await query
                .OrderByDescending(x => x.VoucherId)
                .Skip((currentPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .Select(x => new
                {
                    x.VoucherId,
                    x.Name,
                    x.Code,
                    x.Promotion,
                    x.Description,
                    x.Quantity,
                    x.StartedAt,
                    x.ExpiredAt
                })
                .ToListAsync();

            var voucherItems = vouchers.Select(x =>
            {
                var status = GetVoucherStatus(x.StartedAt, x.ExpiredAt);
                return new AdminVoucherIndexItemViewModel
                {
                    VoucherId = x.VoucherId,
                    Name = x.Name ?? string.Empty,
                    Code = x.Code ?? string.Empty,
                    Promotion = x.Promotion ?? string.Empty,
                    Description = x.Description ?? string.Empty,
                    Quantity = x.Quantity ?? 0,
                    StartedAt = x.StartedAt,
                    ExpiredAt = x.ExpiredAt,
                    StatusLabel = status.Label,
                    StatusTone = status.Tone
                };
            }).ToList();

            var model = new AdminVoucherIndexViewModel
            {
                Code = code,
                Name = name,
                DateFrom = dateFrom,
                DateTo = dateTo,
                Page = currentPage,
                PageSize = resolvedPageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Vouchers = voucherItems
            };

            return View(model);
        }

        [HttpGet("Form")]
        public async Task<IActionResult> Form(int? id, int page = 1, string? code = null, string? name = null, DateTime? dateFrom = null, DateTime? dateTo = null)
        {
            AdminVoucherFormViewModel model;

            if (id.HasValue)
            {
                var voucher = await _voucherService.GetVoucherByIdAsync(id.Value);
                if (voucher == null)
                {
                    return NotFound();
                }

                TryParsePromotion(voucher.Promotion, out var promotionValue, out var promotionType);

                model = new AdminVoucherFormViewModel
                {
                    VoucherId = voucher.VoucherId,
                    Name = voucher.Name ?? string.Empty,
                    Code = voucher.Code ?? string.Empty,
                    PromotionValue = promotionValue,
                    PromotionType = promotionType,
                    Quantity = voucher.Quantity,
                    Description = voucher.Description ?? string.Empty,
                    StartedAt = voucher.StartedAt,
                    ExpiredAt = voucher.ExpiredAt
                };
            }
            else
            {
                model = new AdminVoucherFormViewModel();
            }

            model.ReturnPage = Math.Max(1, page);
            model.ReturnCode = code;
            model.ReturnName = name;
            model.ReturnDateFrom = dateFrom;
            model.ReturnDateTo = dateTo;

            return View(model);
        }

        [HttpPost("Save")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Save(AdminVoucherFormViewModel model)
        {
            if (!await ValidateVoucherFormAsync(model))
            {
                return View("Form", model);
            }

            var normalizedCode = model.Code.Trim();
            var voucher = model.VoucherId > 0
                ? await _voucherService.GetVoucherByIdAsync(model.VoucherId)
                : new Voucher
                {
                    CreatedAt = DateTime.Now
                };

            if (voucher == null)
            {
                return NotFound();
            }

            voucher.Name = model.Name.Trim();
            voucher.Code = normalizedCode;
            voucher.Description = model.Description.Trim();
            voucher.Quantity = model.Quantity;
            voucher.StartedAt = model.StartedAt;
            voucher.ExpiredAt = model.ExpiredAt;
            voucher.Promotion = BuildPromotionDisplay(model.PromotionValue, model.PromotionType);
            voucher.UpdatedAt = DateTime.Now;

            if (model.VoucherId > 0)
            {
                await _voucherService.UpdateVoucherAsync(model.VoucherId, voucher);
            }
            else
            {
                await _voucherService.CreateVoucherAsync(voucher);
            }

            return RedirectToAction(nameof(Index), new
            {
                page = model.ReturnPage,
                code = model.ReturnCode,
                name = model.ReturnName,
                dateFrom = model.ReturnDateFrom?.ToString("yyyy-MM-dd"),
                dateTo = model.ReturnDateTo?.ToString("yyyy-MM-dd")
            });
        }

        [HttpDelete("Delete/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var result = await _voucherService.DeleteVoucherAsync(id);
            if (!result)
            {
                return NotFound(new { success = false, message = "Không tìm thấy voucher với ID đã cho." });
            }

            return Json(new { success = true, message = "Đã xóa mã khuyến mãi." });
        }
    }
}
