using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
	[Area("Admin")]
	[Route("admin")]
    [Authorize(Roles = "Admin")]
    public class HomeController : BaseAdminController
	{
        public HomeController(ApplicationDbContext context) : base(context)
        {
        }

        [Route("")]
		[Route("Index")]
		public IActionResult Index()
		{
			var payments = _context.Payments.Select(x => new {x.PaymentId, x.PaymentDate,x.PaymentMethod,x.Amount,x.Status})
				.OrderByDescending(x => x.PaymentId).Take(7).ToList();
			var new_users = _context.Users.Select( x=> new { x.UserId,x.Img,x.FirstName,x.LastName,x.CreatedAt })
				.OrderByDescending(x=>x.UserId).Take(7).ToList();
            var orders = _context.Orders
                .Include(x=>x.User)
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product) // Include Product để truy cập thông tin sản phẩm
                .Select(order => new
                {
                    order.OrderId,
                    order.User.Email,
                    order.TotalAmount,
                    order.OrderStatus,
                    // Xử lý OrderItems để lấy sản phẩm có giá cao nhất
                    TopProduct = order.OrderItems
                        .OrderByDescending(oi => oi.Product.SellPrice)
                        .Select(oi => oi.Product.Name)
                        .FirstOrDefault(), // Lấy sản phẩm đắt nhất
                    OtherProductsCount = order.OrderItems.Count() - 1 // Số lượng sản phẩm còn lại
                })
                .OrderByDescending(x=>x.OrderId)
                .Take(7)
                .ToList();
            var oneMonthAgo = DateTime.Now.AddMonths(-1); // Lấy ngày cách đây 1 tháng
            var revenue = _context.Payments
                .Where(x => x.Status == "Paid" && x.PaymentDate >= oneMonthAgo && x.PaymentDate <= DateTime.Now)
                .Sum(x => x.Amount);
            
            var order_count = _context.Orders.Count();
			var user_count = _context.Users.Count();
			var products = _context.Products.Count();
            var hotSaleProducts = _context.Products
                .Include(p=>p.OrderItems)
                 .Where(x => x.Visible == true)
                 .OrderByDescending(x => x.OrderItems.Count)
                 .Take(3)
                 .ToList();

            ViewBag.Payments = payments;
			ViewBag.New_users = new_users;
            ViewBag.New_orders = orders;
			ViewBag.Order_count = order_count;
			ViewBag.User_count = user_count;
			ViewBag.Product_count = products;
            ViewBag.Revenue = revenue;
            ViewBag.hotSaleProducts = hotSaleProducts;
			return View();
		}

        [HttpGet("GetOrderStats")]
        public IActionResult GetOrderStats(int month, int year)
        {
            var stats = _context.Orders
                .Where(o => o.OrderDate.Value.Month == month && o.OrderDate.Value.Year == year)
                .GroupBy(o => o.OrderDate.Value.Day)
                .Select(g => new
                {
                    Day = g.Key,
                    Orders = g.Count(),
                    Revenue = g.Sum(o => o.TotalAmount)
                })
                .OrderBy(x => x.Day)
                .ToList();

            return Json(stats);
        }

    }
}
