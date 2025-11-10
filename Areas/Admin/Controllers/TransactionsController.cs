using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.ViewModel;
using static QRCoder.PayloadGenerator;

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
            var transactions = _context.Payments.Include(p=>p.Order).ThenInclude(pp=>pp.User)
                    .OrderBy(p => p.Status.ToLower() == "paid")
                .ThenByDescending(p => p.PaymentId).ToList();
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

        [HttpGet]
        [Route("Filter")]
        public IActionResult Filter(int? transactionId, string? nameCustomer, string? phoneNumber, string? paymentStatus,
             DateTime? dateFrom, DateTime? dateTo, decimal? amountFrom, decimal? amountTo)
        {
            var transactions = _context.Payments
                .Include(x => x.Order)
                .ThenInclude(o => o.User)
                .AsQueryable();

            // Áp dụng các tiêu chí lọc
            if (transactionId.HasValue && transactionId.Value != 0)
                transactions = transactions.Where(p => p.PaymentId == transactionId.Value);

            if (!string.IsNullOrEmpty(nameCustomer))
                transactions = transactions.Where(p => p.Order.User.FirstName.Contains(nameCustomer) || p.Order.User.LastName.Contains(nameCustomer));

            if (!string.IsNullOrEmpty(paymentStatus))
                transactions = transactions.Where(p => p.Status == paymentStatus);


            if (dateFrom.HasValue || dateTo.HasValue)
                transactions = transactions.Where(p =>
                    (!dateFrom.HasValue || p.PaymentDate >= dateFrom.Value) &&
                    (!dateTo.HasValue || p.PaymentDate <= dateTo.Value)
                );
            if (amountFrom.HasValue || amountTo.HasValue)
                transactions = transactions.Where(p =>
                    (!amountFrom.HasValue || p.Amount >= amountFrom.Value) &&
                    (!amountTo.HasValue || p.Amount <= amountTo.Value)
                );
            if (!string.IsNullOrEmpty(phoneNumber))
                transactions = transactions.Where(p => p.Order.User.PhoneNumber.Contains(phoneNumber));


            // Chuyển đổi sang view model để trả về JSON
            var result = transactions
                .OrderBy(p => p.Status.ToLower() == "paid")
                .ThenByDescending(p => p.PaymentId)
                .Take(100)
                .Select(p => new TransactionVM
                {
                    TransId = p.PaymentId,
                    InvoiceId = p.OrderId ?? 0,
                    PaymentMethod = p.PaymentMethod,
                    CustomerName = p.Order != null && p.Order.User != null
                        ? (p.Order.User.FirstName + " " + p.Order.User.LastName)
                        : string.Empty,
                    PhoneNumber = p.Order != null && p.Order.User != null
                        ? p.Order.User.PhoneNumber
                        : string.Empty,
                    Status = p.Status,
                    Amount = p.Amount,
                    PaymentDate = p.PaymentDate
                })
                .ToList();

            return Json(result);
        }
    }
}
