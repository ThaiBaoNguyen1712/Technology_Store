using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using NuGet.Protocol;
using Tech_Store.Models;
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
        public OrdersController(ApplicationDbContext context, NotificationService notificationService) : base(context) {
            _notificationService = notificationService;
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

            var order = await _context.Orders.FirstOrDefaultAsync(o => o.OrderId == id);
            if(order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });

            }
            order.OrderStatus = status;
            await _context.SaveChangesAsync();

            //Gửi thông báo cho người dùng
            await _notificationService.NotifyAsync(Events.NotificationTarget.SpecificUsers, "Trạng thái đơn hàng", $"Đơn hàng {order.OrderId} của bạn đã đổi trạng thái thành {order.OrderStatus}","info"
                , $"/user/myOrders/OrderDetail/{order.OrderId}", new List<int> { order.UserId });

            return Json(new { success = true, message = "Thay đổi thành công" });
        }
    }
}
