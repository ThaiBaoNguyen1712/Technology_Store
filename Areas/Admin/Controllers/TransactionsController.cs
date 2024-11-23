using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class TransactionsController : BaseAdminController
    {
        public TransactionsController(ApplicationDbContext context): base(context) { }
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            var transactions = _context.Payments.Include(p=>p.Order).ThenInclude(pp=>pp.User).OrderByDescending(x=>x.OrderId).ToList();
            return View(transactions);
        }
        [Route("Detail")]
        public IActionResult Detail()
        {
            return View();
        }
        [HttpPost("UpdateStatus")]
        public IActionResult UpdateStatus(int paymentId)
        {
            var payment = _context.Payments.FirstOrDefault(x => x.PaymentId == paymentId);
            if (payment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giao dịch." });
            }

            payment.Status = "Paid";
            _context.SaveChanges();

            return Json(new { success = true });
        }

    }
}
