using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Helpers;
using Tech_Store.Models;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class TransactionsController : BaseAdminController
    {
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;

        public TransactionsController(ApplicationDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
        }

        private sealed class AdminTransactionQueryItem
        {
            public int PaymentId { get; set; }
            public int OrderId { get; set; }
            public string PaymentMethod { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public string Status { get; set; } = string.Empty;
            public DateTime? PaymentDate { get; set; }
            public DateTime? CreatedAt { get; set; }
            public string CustomerName { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Email { get; set; } = string.Empty;
            public string OrderStatus { get; set; } = string.Empty;
        }

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            var resolvedPageSize = pageSize.GetValueOrDefault(FallbackPageSize);
            return resolvedPageSize > 0 ? resolvedPageSize : FallbackPageSize;
        }

        private IQueryable<AdminTransactionQueryItem> BuildTransactionQuery()
        {
            return _context.Payments
                .AsNoTracking()
                .Select(x => new AdminTransactionQueryItem
                {
                    PaymentId = x.Id,
                    OrderId = x.OrderId ?? 0,
                    PaymentMethod = x.Gateway,
                    Amount = x.AmountIn,
                    Status = x.PaymentStatus,
                    PaymentDate = x.TransactionDate,
                    CreatedAt = x.CreatedAt,
                    CustomerName = x.Order != null && x.Order.User != null
                        ? (((x.Order.User.LastName ?? string.Empty) + " " + (x.Order.User.FirstName ?? string.Empty)).Trim())
                        : string.Empty,
                    PhoneNumber = x.Order != null && x.Order.User != null
                        ? (x.Order.User.PhoneNumber ?? string.Empty)
                        : string.Empty,
                    Email = x.Order != null && x.Order.User != null
                        ? x.Order.User.Email
                        : string.Empty,
                    OrderStatus = x.Order != null ? x.Order.OrderStatus : string.Empty
                });
        }

        private static IQueryable<AdminTransactionQueryItem> ApplyTransactionFilters(
            IQueryable<AdminTransactionQueryItem> query,
            int? transactionId,
            int? orderId,
            string? customerName,
            string? phoneNumber,
            string? paymentStatus,
            string? paymentMethod,
            DateTime? dateFrom,
            DateTime? dateTo,
            decimal? amountFrom,
            decimal? amountTo)
        {
            if (transactionId.HasValue)
            {
                query = query.Where(x => x.PaymentId == transactionId.Value);
            }

            if (orderId.HasValue)
            {
                query = query.Where(x => x.OrderId == orderId.Value);
            }

            if (!string.IsNullOrWhiteSpace(customerName))
            {
                var normalizedCustomerName = customerName.Trim();
                query = query.Where(x => x.CustomerName.Contains(normalizedCustomerName));
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                var normalizedPhoneNumber = phoneNumber.Trim();
                query = query.Where(x => x.PhoneNumber.Contains(normalizedPhoneNumber));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(x => x.Status == paymentStatus);
            }

            if (!string.IsNullOrWhiteSpace(paymentMethod))
            {
                query = query.Where(x => x.PaymentMethod == paymentMethod);
            }

            if (dateFrom.HasValue)
            {
                var fromDate = dateFrom.Value.Date;
                query = query.Where(x => (x.PaymentDate ?? x.CreatedAt).HasValue && (x.PaymentDate ?? x.CreatedAt) >= fromDate);
            }

            if (dateTo.HasValue)
            {
                var toDateExclusive = dateTo.Value.Date.AddDays(1);
                query = query.Where(x => (x.PaymentDate ?? x.CreatedAt).HasValue && (x.PaymentDate ?? x.CreatedAt) < toDateExclusive);
            }

            if (amountFrom.HasValue)
            {
                query = query.Where(x => x.Amount >= amountFrom.Value);
            }

            if (amountTo.HasValue)
            {
                query = query.Where(x => x.Amount <= amountTo.Value);
            }

            return query;
        }

        private static string GetPaymentMethodLabel(string paymentMethod)
        {
            return paymentMethod switch
            {
                "COD" => "TT khi nhận hàng (COD)",
                "MoMo" => "Thanh toán qua MoMo",
                "VNPay" => "Thanh toán qua VNPay",
                "cash" => "Thanh toán tiền mặt",
                _ => string.IsNullOrWhiteSpace(paymentMethod) ? "-" : paymentMethod
            };
        }

        private static string GetPaymentMethodAsset(string paymentMethod)
        {
            return paymentMethod switch
            {
                "COD" => "/Upload/Logo/shipcod.png",
                "MoMo" => "/Upload/Logo/LogoMoMo.webp",
                "VNPay" => "/Upload/Logo/LogoVNPay.png",
                _ => string.Empty
            };
        }

        private static string GetPaymentStatusLabel(string status)
        {
            return string.Equals(status, "Paid", StringComparison.OrdinalIgnoreCase)
                ? "Đã thanh toán"
                : "Chưa thanh toán";
        }

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(
            int? transactionId,
            int? orderId,
            string? customerName,
            string? phoneNumber,
            string? paymentStatus,
            string? paymentMethod,
            DateTime? dateFrom,
            DateTime? dateTo,
            decimal? amountFrom,
            decimal? amountTo,
            int page = 1,
            int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            if (resolvedPageSize <= 0)
            {
                resolvedPageSize = GetDefaultAdminPageSize();
            }

            var query = ApplyTransactionFilters(
                BuildTransactionQuery(),
                transactionId,
                orderId,
                customerName,
                phoneNumber,
                paymentStatus,
                paymentMethod,
                dateFrom,
                dateTo,
                amountFrom,
                amountTo);

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);
            var currentPage = Math.Min(Math.Max(1, page), totalPages);

            var transactionRows = await query
                .OrderByDescending(x => x.CreatedAt)
                .ThenByDescending(x => x.PaymentId)
                .Skip((currentPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .ToListAsync();

            var transactions = transactionRows
                .Select(x => new AdminTransactionListItemViewModel
                {
                    PaymentId = x.PaymentId,
                    OrderId = x.OrderId,
                    CustomerName = x.CustomerName,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,
                    PaymentMethod = x.PaymentMethod,
                    PaymentMethodLabel = PaymentDisplayHelper.GetLabel(x.PaymentMethod),
                    PaymentMethodAsset = PaymentDisplayHelper.GetLogoUrl(x.PaymentMethod),
                    Status = x.Status,
                    PaymentStatusLabel = GetPaymentStatusLabel(x.Status),
                    Amount = x.Amount,
                    PaymentDate = x.PaymentDate,
                    CreatedAt = x.CreatedAt
                })
                .ToList();

            var model = new AdminTransactionIndexViewModel
            {
                Transactions = transactions,
                TransactionId = transactionId,
                OrderId = orderId,
                CustomerName = customerName,
                PhoneNumber = phoneNumber,
                PaymentStatus = paymentStatus,
                PaymentMethod = paymentMethod,
                DateFrom = dateFrom,
                DateTo = dateTo,
                AmountFrom = amountFrom,
                AmountTo = amountTo,
                Page = currentPage,
                PageSize = resolvedPageSize,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(model);
        }

        [HttpGet("QuickDrawer/{id}")]
        public async Task<IActionResult> QuickDrawer(int id)
        {
            var payment = await _context.Payments
                .AsNoTracking()
                .Where(x => x.Id == id)
                .Select(x => new AdminTransactionQuickDrawerViewModel
                {
                    PaymentId = x.Id,
                    OrderId = x.OrderId ?? 0,
                    CustomerName = x.Order != null && x.Order.User != null
                        ? (((x.Order.User.LastName ?? string.Empty) + " " + (x.Order.User.FirstName ?? string.Empty)).Trim())
                        : string.Empty,
                    PhoneNumber = x.Order != null && x.Order.User != null ? x.Order.User.PhoneNumber ?? string.Empty : string.Empty,
                    Email = x.Order != null && x.Order.User != null ? x.Order.User.Email : string.Empty,
                    PaymentMethod = x.Gateway,
                    Status = x.PaymentStatus,
                    Amount = x.AmountIn,
                    PaymentDate = x.TransactionDate,
                    CreatedAt = x.CreatedAt,
                    OrderStatus = x.Order != null ? x.Order.OrderStatus : string.Empty
                })
                .FirstOrDefaultAsync();

            if (payment == null)
            {
                return NotFound();
            }

            payment.PaymentMethodLabel = PaymentDisplayHelper.GetLabel(payment.PaymentMethod);
            payment.PaymentMethodAsset = PaymentDisplayHelper.GetLogoUrl(payment.PaymentMethod);
            payment.PaymentStatusLabel = GetPaymentStatusLabel(payment.Status);
            payment.OrderDetailUrl = payment.OrderId > 0
                ? $"/Admin/Orders/View/{payment.OrderId}"
                : "/Admin/Orders";

            return PartialView("_QuickDrawerContent", payment);
        }

        [HttpPost("UpdateStatus")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int paymentId)
        {
            var payment = await _context.Payments.FirstOrDefaultAsync(x => x.Id == paymentId);
            if (payment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giao dịch." });
            }

            if (string.Equals(payment.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Giao dịch này đã được xác nhận thanh toán." });
            }

            payment.PaymentStatus = "Paid";
            payment.TransactionDate ??= DateTime.Now;
            payment.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "Đã xác nhận thanh toán.",
                paymentStatusLabel = GetPaymentStatusLabel(payment.PaymentStatus),
                paymentDate = payment.TransactionDate?.ToString("dd/MM/yyyy HH:mm:ss")
            });
        }

        [HttpGet("Detail")]
        public IActionResult Detail()
        {
            return View();
        }
    }
}
