using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Tech_Store.Models;
using Tech_Store.Models.DTO;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class OrdersController : BaseAdminController
    {
      
        public OrdersController(ApplicationDbContext context) :base(context) { }

        [Route("{status}")]
        [Route("Index/{status}")]
        [HttpGet]
        public async Task<IActionResult> Index(string? status)
        {
            var query = _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.VarientProducts) // Include VariantProduct from Product
                .Include(x => x.Payments)
                .Include(x => x.ShippingAddress)
                .Include(x => x.User)
                .OrderByDescending(x => x.OrderId)
                .AsQueryable();

            if (!string.IsNullOrEmpty(status) && status != "all")
            {
                query = query.Where(x => x.OrderStatus == status);
            }

            var orders = await query.ToListAsync();
            return View(orders);
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

    }
}
