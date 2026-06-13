using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Protocol;
using Tech_Store.Helpers;
using Tech_Store.Models;
using Tech_Store.Services;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.NotificationServices;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class OrdersController : BaseAdminController
    {
        private readonly NotificationService _notificationService;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;

        public OrdersController(ApplicationDbContext context, NotificationService notificationService, IEmailService emailService, IConfiguration configuration) : base(context)
        {
            _notificationService = notificationService;
            _emailService = emailService;
            _configuration = configuration;
        }

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            return pageSize.GetValueOrDefault(FallbackPageSize) > 0 ? pageSize.Value : FallbackPageSize;
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        [Route("Index/{status?}")]
        [Route("{status?}")]
        public async Task<IActionResult> Index([FromRoute(Name = "status")] string? routeStatus, [FromQuery(Name = "status")] string? queryStatus, string? keyword, string? paymentStatus, DateTime? dateFrom, DateTime? dateTo, decimal? amountFrom, decimal? amountTo, int page = 1, int? pageSize = null)
        {
            var status = !string.IsNullOrWhiteSpace(queryStatus) ? queryStatus : routeStatus;
            if (string.Equals(status, "all", StringComparison.OrdinalIgnoreCase))
            {
                status = null;
            }

            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var query = _context.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(x => x.Payments)
                .Include(x => x.User)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status))
            {
                query = query.Where(x => x.OrderStatus == status);
            }

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    x.OrderId.ToString().Contains(normalizedKeyword) ||
                    ((x.User.FirstName ?? string.Empty) + " " + (x.User.LastName ?? string.Empty)).Contains(normalizedKeyword) ||
                    (x.User.PhoneNumber != null && x.User.PhoneNumber.Contains(normalizedKeyword)) ||
                    x.OrderItems.Any(oi => oi.Product.Name.Contains(normalizedKeyword)) ||
                    x.OrderItems.Any(oi => oi.VarientProduct.Sku.Contains(normalizedKeyword)));
            }

            if (!string.IsNullOrWhiteSpace(paymentStatus))
            {
                query = query.Where(x => x.Payments.Any(p => p.PaymentStatus == paymentStatus));
            }

            if (dateFrom.HasValue)
            {
                query = query.Where(x => x.OrderDate >= dateFrom.Value.Date);
            }

            if (dateTo.HasValue)
            {
                var inclusiveDateTo = dateTo.Value.Date.AddDays(1).AddTicks(-1);
                query = query.Where(x => x.OrderDate <= inclusiveDateTo);
            }

            if (amountFrom.HasValue)
            {
                query = query.Where(x => x.TotalAmount >= amountFrom.Value);
            }

            if (amountTo.HasValue)
            {
                query = query.Where(x => x.TotalAmount <= amountTo.Value);
            }

            var totalItems = await query.CountAsync();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)resolvedPageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            var orders = await query
                .OrderByDescending(x => x.CreatedAt ?? x.OrderDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.OrderId)
                .Skip((page - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .ToListAsync();

            var model = new AdminOrderIndexViewModel
            {
                Keyword = keyword,
                Status = status,
                PaymentStatus = paymentStatus,
                DateFrom = dateFrom,
                DateTo = dateTo,
                AmountFrom = amountFrom,
                AmountTo = amountTo,
                Page = page,
                PageSize = resolvedPageSize,
                TotalItems = totalItems,
                TotalPages = totalPages,
                Orders = orders.Select(order =>
                {
                    var payment = order.Payments
                        .OrderByDescending(x => x.CreatedAt)
                        .FirstOrDefault();
                    var (orderBadgeClass, orderLabel) = GetOrderStatusPresentation(order.OrderStatus);
                    var (paymentBadgeClass, paymentLabel) = GetPaymentStatusPresentation(payment?.PaymentStatus);

                    return new AdminOrderIndexItemViewModel
                    {
                        OrderId = order.OrderId,
                        CustomerName = $"{order.User.LastName} {order.User.FirstName}".Trim(),
                        PhoneNumber = order.User.PhoneNumber,
                        Email = order.User.Email,
                        OrderDate = order.OrderDate,
                        OrderStatus = order.OrderStatus,
                        OrderStatusLabel = orderLabel,
                        OrderStatusBadgeClass = orderBadgeClass,
                        PaymentStatus = payment?.PaymentStatus ?? "Unpaid",
                        PaymentStatusLabel = paymentLabel,
                        PaymentStatusBadgeClass = paymentBadgeClass,
                        PaymentMethod = PaymentDisplayHelper.GetLabel(payment?.Gateway),
                        TotalAmount = order.TotalAmount,
                        ItemCount = order.OrderItems.Sum(x => x.Quantity),
                        ProductSummary = BuildProductSummary(order)
                    };
                }).ToList()
            };

            return View(model);
        }

        [Route("View/{id}")]
        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var model = await BuildOrderDetailViewModelAsync(id);
            if (model == null)
            {
                return BadRequest("Không tìm thấy");
            }

            return View(model);
        }

        [HttpGet("QuickDrawer/{id}")]
        public async Task<IActionResult> QuickDrawer(int id)
        {
            var model = await BuildOrderDetailViewModelAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            return PartialView("_QuickDrawerContent", model);
        }

        [HttpPost("ChangeStatus")]
        public async Task<JsonResult> ChangeStatus(string status,int id)
        {
            if(status == null || id < 0)
            {
                return Json(new { success = false, message = "Đã có lỗi khi truyền dữ liệu" });
            }

            var order = await _context.Orders
                .Include(p=>p.User)
                .Include(p=>p.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
            if(order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            }
            order.OrderStatus = status;
            await _context.SaveChangesAsync();


            //Gửi thông báo cho người dùng
            await _notificationService.NotifyAsync(Events.NotificationTarget.SpecificUsers, "Trạng thái đơn hàng", $"Đơn hàng {order.OrderId} của bạn đã đổi trạng thái thành {order.OrderStatus}","info"
                , $"/user/myOrders/OrderDetail/{order.OrderId}", new List<int> { order.UserId });

            if(status == "Completed" || status =="completed")
            {
                try
                {
                    //using task thread to send email
                    await SendEmailToCusOrderCompleted(order);
                    return Json(new { success = true, message = "Đơn hàng đã được hoàn tất và chúng tôi đã gửi Email hóa đơn cho khách hàng" });
                }
                catch (Exception ex)
                {
                    return Json(new { success = true, message = "Đơn hàng đã được hoàn tất nhưng chúng tôi chưa thể gửi được Email hóa đơn cho khách hàng" });
                }
               
            }
            return Json(new { success = true, message = "Thay đổi thành công" });
        }
        //Gửi email tới khách hàng khi hoàn tất và đã thanh toán đơn hàng
        private async Task SendEmailToCusOrderCompleted(Order order)
        {
            var hydratedOrder = await _context.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.VarientProduct)
                        .ThenInclude(vp => vp.VariantAttributes)
                            .ThenInclude(va => va.AttributeValue)
                .Include(x => x.Payments)
                .Include(x => x.ShippingAddress)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrderId == order.OrderId);

            if (hydratedOrder == null || string.IsNullOrWhiteSpace(hydratedOrder.User.Email))
            {
                return;
            }

            var invoiceEmail = await BuildInvoiceEmailAsync(hydratedOrder);
            await _emailService.SendEmailOrderCompleted(invoiceEmail);
        }

        private async Task<AdminOrderDetailViewModel?> BuildOrderDetailViewModelAsync(int id)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.VarientProduct)
                        .ThenInclude(vp => vp.VariantAttributes)
                            .ThenInclude(va => va.AttributeValue)
                .Include(x => x.Payments)
                .Include(x => x.ShippingAddress)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
            {
                return null;
            }

            var payment = order.Payments
                .OrderByDescending(x => x.TransactionDate ?? x.CreatedAt)
                .FirstOrDefault();
            var address = await BuildFullAddressAsync(order.ShippingAddress);
            var (orderBadgeClass, orderLabel) = GetOrderStatusPresentation(order.OrderStatus);
            var (paymentBadgeClass, paymentLabel) = GetPaymentStatusPresentation(payment?.PaymentStatus);

            return new AdminOrderDetailViewModel
            {
                OrderId = order.OrderId,
                CustomerName = $"{order.User.LastName} {order.User.FirstName}".Trim(),
                PhoneNumber = order.User.PhoneNumber,
                Email = order.User.Email,
                Address = address,
                Note = order.Note,
                OrderDate = order.OrderDate,
                UpdatedAt = order.UpdatedAt,
                OrderStatus = order.OrderStatus,
                OrderStatusLabel = orderLabel,
                OrderStatusBadgeClass = orderBadgeClass,
                PaymentStatus = payment?.PaymentStatus ?? "Unpaid",
                PaymentStatusLabel = paymentLabel,
                PaymentStatusBadgeClass = paymentBadgeClass,
                PaymentMethod = PaymentDisplayHelper.GetLabel(payment?.Gateway),
                PaymentDate = payment?.TransactionDate,
                ItemCount = order.OrderItems.Sum(x => x.Quantity),
                OriginAmount = order.OriginAmount ?? order.OrderItems.Sum(x => x.Price * x.Quantity),
                DiscountAmount = order.DiscountAmount ?? 0,
                DeductAmount = order.DeductAmount ?? 0,
                ShippingAmount = order.ShippingAmount ?? 0,
                TotalAmount = order.TotalAmount,
                Items = order.OrderItems.Select(item => new AdminOrderDetailItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.Product.Name,
                    ProductImage = item.Product.Image,
                    VariantSku = item.VarientProduct.Sku,
                    VariantDisplay = BuildVariantDisplay(item.VarientProduct),
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    LineTotal = item.Price * item.Quantity
                }).ToList()
            };
        }

        private static (string badgeClass, string label) GetOrderStatusPresentation(string? status)
        {
            return status switch
            {
                "Pending" => ("badge-primary", "Đang chờ"),
                "Confirmed" => ("badge-info", "Đã xác nhận"),
                "Shipping" => ("badge-warning", "Đang giao hàng"),
                "Delivered" => ("badge-success", "Đã giao hàng"),
                "Completed" => ("badge-success", "Hoàn thành"),
                "Cancelled" => ("badge-danger", "Đã hủy"),
                "Refunded" => ("badge-secondary", "Đã hoàn tiền"),
                _ => ("badge-light text-dark border", "Không xác định")
            };
        }

        private static (string badgeClass, string label) GetPaymentStatusPresentation(string? status)
        {
            return status switch
            {
                "Paid" => ("badge-success", "Đã thanh toán"),
                "Unpaid" => ("badge-danger", "Chưa thanh toán"),
                _ => ("badge-light text-dark border", "Chưa cập nhật")
            };
        }

        private static string BuildProductSummary(Order order)
        {
            var productNames = order.OrderItems
                .Select(x => x.Product.Name)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct()
                .ToList();

            if (productNames.Count == 0)
            {
                return "-";
            }

            if (productNames.Count <= 2)
            {
                return string.Join(", ", productNames);
            }

            return $"{string.Join(", ", productNames.Take(2))} +{productNames.Count - 2}";
        }

        private static string BuildVariantDisplay(VarientProduct variant)
        {
            var values = variant.VariantAttributes
                .OrderBy(x => x.VariantAttributeId)
                .Select(x => x.AttributeValue.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (values.Count > 0)
            {
                return string.Join(" / ", values);
            }

            return string.IsNullOrWhiteSpace(variant.Attributes) ? "-" : variant.Attributes;
        }

        private async Task<InvoiceEmail> BuildInvoiceEmailAsync(Order order)
        {
            var settings = await _context.Settings.AsNoTracking().ToListAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var logoUrl = GetAbsoluteLogoUrl(baseUrl, settings.FirstOrDefault(x => x.Key == "LogoUrl")?.Value);
            var customerAddress = await BuildFullAddressAsync(order.ShippingAddress);
            var payment = order.Payments
                .OrderByDescending(x => x.TransactionDate ?? x.CreatedAt)
                .FirstOrDefault();

            return new InvoiceEmail
            {
                ToEmail = order.User.Email,
                CustomerName = $"{order.User.LastName} {order.User.FirstName}".Trim(),
                CustomerAddress = customerAddress,
                CustomerPhone = order.User.PhoneNumber ?? string.Empty,
                InvoiceNumber = order.OrderId.ToString(),
                OrderDate = order.OrderDate,
                Products = order.OrderItems.Select(orderItem => new ProductItem
                {
                    Name = BuildEmailProductName(orderItem),
                    Quantity = orderItem.Quantity,
                    Price = orderItem.Price
                }).ToList(),
                ShippingFee = order.ShippingAmount ?? 0,
                ShippingAmount = order.ShippingAmount ?? 0,
                PaymentMethod = PaymentDisplayHelper.GetLabel(payment?.Gateway),
                IsPaid = string.Equals(payment?.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase),
                CompanyName = settings.FirstOrDefault(x => x.Key == "NameCompany")?.Value ?? settings.FirstOrDefault(x => x.Key == "NameWebsite")?.Value ?? "Tech Store",
                CompanyAddress = settings.FirstOrDefault(x => x.Key == "Address")?.Value ?? string.Empty,
                CompanyEmail = settings.FirstOrDefault(x => x.Key == "Email")?.Value ?? string.Empty,
                CompanyPhone = settings.FirstOrDefault(x => x.Key == "PhoneNumber")?.Value ?? string.Empty,
                CompanyWebsite = baseUrl,
                SupportEmail = settings.FirstOrDefault(x => x.Key == "Email")?.Value ?? string.Empty,
                SupportPhone = settings.FirstOrDefault(x => x.Key == "PhoneNumber")?.Value ?? string.Empty,
                InvoicePdfUrl = $"{baseUrl}/Admin/POS/Print-Invoice?id={order.OrderId}",
                LogoPath = logoUrl,
                Subject = $"Cảm ơn quý khách - Hóa đơn #{order.OrderId}",
                OriginAmount = order.OriginAmount ?? order.OrderItems.Sum(x => x.Price * x.Quantity),
                DiscountAmount = order.DiscountAmount ?? 0,
                DeductAmount = order.DeductAmount ?? 0,
                TotalAmount = order.TotalAmount
            };
        }

        private static string BuildEmailProductName(OrderItem orderItem)
        {
            var variantDisplay = BuildVariantDisplay(orderItem.VarientProduct);
            return variantDisplay == "-"
                ? orderItem.Product.Name
                : $"{orderItem.Product.Name} ({variantDisplay})";
        }

        private static string GetAbsoluteLogoUrl(string baseUrl, string? logoPath)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(logoPath, UriKind.Absolute, out _))
            {
                return logoPath;
            }

            return $"{baseUrl}/Upload/Logo/{logoPath.TrimStart('/')}";
        }

        private async Task<string> BuildFullAddressAsync(Address? shippingAddress)
        {
            if (shippingAddress == null)
            {
                return "-";
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(shippingAddress.AddressLine))
            {
                parts.Add(shippingAddress.AddressLine.Trim());
            }

            if (string.IsNullOrWhiteSpace(shippingAddress.Province) ||
                string.IsNullOrWhiteSpace(shippingAddress.District) ||
                string.IsNullOrWhiteSpace(shippingAddress.Ward))
            {
                return parts.Count > 0 ? string.Join(", ", parts) : "-";
            }

            var jsonString = await System.IO.File.ReadAllTextAsync("wwwroot/Province_VN.json");
            var provinces = JsonConvert.DeserializeObject<List<Province>>(jsonString);

            var province = provinces?.FirstOrDefault(p => p.Code == int.Parse(shippingAddress.Province));
            var district = province?.Districts?.FirstOrDefault(d => d.Code == int.Parse(shippingAddress.District));
            var ward = district?.Wards?.FirstOrDefault(w => w.Code == int.Parse(shippingAddress.Ward));

            if (!string.IsNullOrWhiteSpace(ward?.Name))
            {
                parts.Add(ward.Name);
            }

            if (!string.IsNullOrWhiteSpace(district?.Name))
            {
                parts.Add(district.Name);
            }

            if (!string.IsNullOrWhiteSpace(province?.Name))
            {
                parts.Add(province.Name);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }

    }
}
