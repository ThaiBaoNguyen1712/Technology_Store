using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Protocol;
using Tech_Store.Helpers;
using Tech_Store.Models;
using Tech_Store.Services;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.NotificationServices;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class OrdersController : BaseAdminController
    {
        private readonly NotificationService _notificationService;
        private readonly IEmailService _emailService;

        public OrdersController(ApplicationDbContext context, NotificationService notificationService, IEmailService emailService) : base(context)
        {
            _notificationService = notificationService;
            _emailService = emailService;
        }

        [Route("{status}")]
        [Route("Index/{status}")]
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.VarientProducts)
                .Include(x => x.Payments)
                .Include(x => x.ShippingAddress)
                .Include(x => x.User)
                .OrderByDescending(x => x.OrderId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(x => x.OrderStatus == status);
            }

            var list_cate = _context.Categories.ToList();
            ViewBag.cate = list_cate;
            var list_brand = _context.Brands.ToList();
            ViewBag.brand = list_brand;
            var orders = await query.ToListAsync();
            return View(orders);
        }

        [HttpPost]
        [Route("Filter")]
        public IActionResult Filter(int? orderId, string? nameCustomer, string? paymentStatus ,string? status,
             DateTime? dateFrom, DateTime? dateTo, string? phoneNumber,decimal? amountFrom, decimal? amountTo)
        {
            var orders = _context.Orders
                .Include(p => p.User)
                .Include(p => p.OrderItems)
                .ThenInclude(t => t.Product)
                .Include(p => p.Payments)
                .AsQueryable();

            // Áp dụng các tiêu chí lọc
            if (orderId.HasValue && orderId.Value != 0)
                orders = orders.Where(p => p.OrderId == orderId.Value);

            if (!string.IsNullOrEmpty(nameCustomer))
                orders = orders.Where(p => p.User.FirstName.Contains(nameCustomer) || p.User.LastName.Contains(nameCustomer));

            if (!string.IsNullOrEmpty(status))
                orders = orders.Where(p => p.OrderStatus == status);

   
            if (dateFrom.HasValue || dateTo.HasValue)
                orders = orders.Where(p =>
                    (!dateFrom.HasValue || p.OrderDate >= dateFrom.Value) &&
                    (!dateTo.HasValue || p.OrderDate <= dateTo.Value)
                );
            if (amountFrom.HasValue || amountTo.HasValue)
                orders = orders.Where(p =>
                    (!amountFrom.HasValue || p.TotalAmount >= amountFrom.Value) &&
                    (!amountTo.HasValue || p.TotalAmount <= amountTo.Value)
                );
            if (!string.IsNullOrEmpty(phoneNumber))
                orders = orders.Where(p => p.User.PhoneNumber.Contains(phoneNumber));

            if (!string.IsNullOrEmpty(paymentStatus))
                orders = orders.Where(p => p.Payments.Select(s=>s.Status).Contains(paymentStatus));

            // Chuyển đổi sang view model để trả về JSON
            var result = orders.OrderByDescending(p => p.OrderId)
                .Take(100)
                .Select(p => new OrderViewModel
                {
                    OrderId = p.OrderId,
                    NameCustomer = p.User.LastName + " " + p.User.FirstName,
                    PhoneNumber = p.User.PhoneNumber,
                    OrderStatus = p.OrderStatus,
                    TotalAmount = p.TotalAmount,
                    ListProducts = string.Join(", ", p.OrderItems.Select(s => s.Product.Name)),
                    PaymentStatus = string.Join(",", p.Payments.Select(s=>s.Status)),
                    OrderDate = p.OrderDate.Value // Đảm bảo OrderDate không null
                })
                .ToList();

            return Json(result);
        }

        [Route("View/{id}")]
        [HttpGet]
        public async  Task<IActionResult> View(int id)
        {

            var order = await _context.Orders
              .Include(x => x.OrderItems)
                  .ThenInclude(oi => oi.Product)
                      .ThenInclude(p => p.VarientProducts) // Include VariantProduct from Product
              .Include(x => x.Payments).Include(x => x.ShippingAddress)
              .Include(x => x.User)
              .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
            {
                return BadRequest("Không tìm thấy");
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(x => x.OrderId == id);
            ViewBag.Payment = payment;

            // Read the JSON file
            var jsonString = await System.IO.File.ReadAllTextAsync("wwwroot/Province_VN.json");
            var provinces = JsonConvert.DeserializeObject<List<Province>>(jsonString);

            // Find the province, district, and ward by ID
            var province = provinces?.FirstOrDefault(p => p.Code == int.Parse(order.ShippingAddress.Province));
            var district = province?.Districts?.FirstOrDefault(d => d.Code == int.Parse(order.ShippingAddress.District));
            var ward = district?.Wards?.FirstOrDefault(w => w.Code == int.Parse(order.ShippingAddress.Ward));

            // Assign to ViewBag
            ViewBag.Address = $"{order.ShippingAddress.AddressLine},{ward?.Name}, {district?.Name}, {province?.Name}";

            return View(order);
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
         
            var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == order.UserId);
            if (!string.IsNullOrEmpty(address.AddressLine) &&
                !string.IsNullOrEmpty(address.Province) &&
                !string.IsNullOrEmpty(address.Ward) &&
                !string.IsNullOrEmpty(address.District))
            {
                // Read the JSON file
                var jsonString = await System.IO.File.ReadAllTextAsync("wwwroot/Province_VN.json");
                var provinces = JsonConvert.DeserializeObject<List<Province>>(jsonString);

                // Find the province, district, and ward by ID
                var province = provinces?.FirstOrDefault(p => p.Code == int.Parse(address.Province));
                var district = province?.Districts?.FirstOrDefault(d => d.Code == int.Parse(address.District));
                var ward = district?.Wards?.FirstOrDefault(w => w.Code == int.Parse(address.Ward));
                // Assign to ViewBag
                ViewBag.Address = $"{address.AddressLine},{ward?.Name}, {district?.Name}, {province?.Name}";
            }
            
            var payment = await _context.Payments.FirstOrDefaultAsync(s=>s.OrderId == order.OrderId);
            var company_infomation = await _context.Settings.ToListAsync();
            List<ProductItem> ListProducts = new();

            foreach (var orderitem in order.OrderItems)
            {
                ListProducts.Add(new ProductItem
                {
                    Name = orderitem.Product.Name,
                    Quantity = orderitem.Quantity,
                    Price = orderitem.Price
                });
            }
            //Lấy URL gốc
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            //Lấy Logo 
            var logoUrl = company_infomation.Where(w => w.Key == "LogoUrl").Select(s => s.Value).FirstOrDefault();
            var invoiceEmail = new InvoiceEmail
            {
                ToEmail = order.User.Email,
                CustomerName = order.User.LastName + " " + order.User.FirstName,
                CustomerAddress = address.ToString(),
                CustomerPhone = order.User.PhoneNumber,
                InvoiceNumber = order.OrderId.ToString(),
                Products = ListProducts,
                ShippingFee = 30000,
                PaymentMethod = payment.PaymentMethod,
                IsPaid = true,
                CompanyName = company_infomation.Where(w => w.Key == "NameCompany").Select(s=>s.Value).FirstOrDefault(),
                CompanyAddress = company_infomation.Where(w => w.Key == "Address").Select(s => s.Value).FirstOrDefault(),
                CompanyEmail = company_infomation.Where(w => w.Key == "Email").Select(s => s.Value).FirstOrDefault(),
                CompanyPhone = company_infomation.Where(w => w.Key == "PhoneNumber").Select(s => s.Value).FirstOrDefault(),
                CompanyWebsite = "techshop-c4cafccbh0dmcwdp.eastasia-01.azurewebsites.net",
                SupportEmail = company_infomation.Where(w => w.Key == "Email").Select(s => s.Value).FirstOrDefault(),
                SupportPhone = company_infomation.Where(w => w.Key == "PhoneNumber").Select(s => s.Value).FirstOrDefault(),
                InvoicePdfUrl = "https://abc.com/invoices/INV-2025-001.pdf",
                LogoPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload","Logo", logoUrl),
                Subject = $"Cảm ơn quý khách - Hóa đơn #{order.OrderId}"
            };

            await _emailService.SendEmailOrderCompleted(invoiceEmail);
        }

    }
}
