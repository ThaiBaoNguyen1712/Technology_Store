using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Tech_Store.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    public abstract class BaseAdminController : Controller
    {
        protected readonly ApplicationDbContext _context;

        protected BaseAdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Override OnActionExecuting to load shared data before every action
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            base.OnActionExecuting(context);

            // Load common data for all actions
            LoadUser();
            LoadLayout();
        }

        protected virtual void LoadUser()
        {
            if (!User.Identity.IsAuthenticated)
            {
                ViewBag.User = null;
                return;
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                ViewBag.User = null;
                return;
            }

            var user = _context.Users
                .Include(u => u.Addresses)
                .FirstOrDefault(u => u.UserId == int.Parse(userId));

            ViewBag.User = user;
        }

        protected virtual void LoadLayout()
        {
            //Count Orders
            var orders = _context.Orders.ToList();
            ViewBag.Order_all = orders.Count;
            ViewBag.Order_pending = orders.Count(x => x.OrderStatus.ToLower() == "pending");
            ViewBag.Order_confirmed = orders.Count(x => x.OrderStatus.ToLower() == "confirmed");
            ViewBag.Order_shipping = orders.Count(x => x.OrderStatus.ToLower() == "shipping");
            ViewBag.Order_delivered = orders.Count(x => x.OrderStatus.ToLower() == "delivered");
            ViewBag.Order_completed = orders.Count(x => x.OrderStatus.ToLower() == "completed");
            ViewBag.Order_cancelled = orders.Count(x => x.OrderStatus.ToLower() == "cancelled");
            ViewBag.Order_refunded = orders.Count(x => x.OrderStatus.ToLower() == "refunded");

            //Count Product Status
            var products = _context.Products.ToList();
            ViewBag.Product_all = products.Count;
            ViewBag.Product_available = products.Count(x => x.Status == "available");
            ViewBag.Product_outstock = products.Count(x => x.Status == "outstock");
            ViewBag.Product_preorder = products.Count(x => x.Status == "preorder");
            ViewBag.Product_discontinued = products.Count(x => x.Status == "discontinued");


        }
    }
}
