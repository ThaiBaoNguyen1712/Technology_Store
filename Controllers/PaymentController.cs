using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Tech_Store.Models;
using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.Enums;
using Tech_Store.Services.Payment;

namespace Tech_Store.Controllers
{
    [Authorize]
    [Route("Payment")]
    public class PaymentController : BaseController
    {
        private readonly IClientPaymentService _clientPaymentService;
        private readonly ISePayService _sePayService;

        public PaymentController(
            ApplicationDbContext context,
            IClientPaymentService clientPaymentService,
            ISePayService sePayService) : base(context)
        {
            _clientPaymentService = clientPaymentService;
            _sePayService = sePayService;
        }

        [Route("Checkout")]
        public async Task<IActionResult> Checkout(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User không hợp lệ");
            }

            var result = await _clientPaymentService.BuildCheckoutPageAsync(
                userId.Value,
                HttpContext.Session.GetString("CartItems"),
                cancellationToken);

            if (!result.Success)
            {
                return StatusCode(result.StatusCode, result.ErrorMessage);
            }

            ViewBag.Address = result.FormattedAddress;
            return View(result.Model);
        }

        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout(PaymentDTo model, string paymentMethod, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Dữ liệu thanh toán không hợp lệ." });
            }

            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User không hợp lệ");
            }

            var validationResult = await _clientPaymentService.ValidatePaymentMethodAsync(paymentMethod, cancellationToken);
            if (!validationResult.Success)
            {
                return StatusCode(validationResult.StatusCode, new { success = false, message = validationResult.ErrorMessage });
            }

            if (validationResult.PaymentMethodType == PaymentMethodType.Cod)
            {
                var codResult = await _clientPaymentService.CreateCodOrderAsync(model, userId.Value, HttpContext, cancellationToken);
                if (!codResult.Success)
                {
                    return StatusCode(codResult.StatusCode, new { success = false, message = codResult.ErrorMessage });
                }

                return Json(new { success = true, redirectTo = Url.Action("OrderSuccess") });
            }

            var onlineResult = await _clientPaymentService.CreateOnlinePaymentAsync(
                validationResult.PaymentMethodType!.Value,
                model,
                userId.Value,
                HttpContext,
                cancellationToken);

            if (!onlineResult.Success)
            {
                return StatusCode(onlineResult.StatusCode, new { success = false, message = onlineResult.ErrorMessage });
            }

            return validationResult.PaymentMethodType == PaymentMethodType.Momo
                ? Json(new { momoPayUrl = onlineResult.RedirectUrl })
                : validationResult.PaymentMethodType == PaymentMethodType.VnPay
                    ? Json(new { vnPayUrl = onlineResult.RedirectUrl })
                    : Json(new { sePayUrl = onlineResult.RedirectUrl });
        }

        [Route("MoMoPayCallBack")]
        public async Task<IActionResult> MoMoPayCallBack(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User không hợp lệ");
            }

            var result = await _clientPaymentService.HandleMomoCallbackAsync(userId.Value, HttpContext, cancellationToken);
            if (result.IsGatewayFailure)
            {
                TempData["Message"] = result.GatewayFailureMessage;
                return RedirectToAction("PaymentFail");
            }

            if (!result.Success)
            {
                TempData["Message"] = result.ErrorMessage;
                return RedirectToAction("OrderFail");
            }

            TempData["Message"] = "Thanh toán MoMo thành công";
            return RedirectToAction("PaymentSuccess");
        }

        [Route("VnPayCallBack")]
        public async Task<IActionResult> VnPayCallBack(CancellationToken cancellationToken)
        {
            var userId = GetCurrentUserId();
            if (userId == null)
            {
                return Unauthorized("User không hợp lệ");
            }

            var result = await _clientPaymentService.HandleVnPayCallbackAsync(userId.Value, HttpContext, cancellationToken);
            if (result.IsGatewayFailure)
            {
                TempData["Message"] = result.GatewayFailureMessage;
                return RedirectToAction("PaymentFail");
            }

            if (!result.Success)
            {
                TempData["Message"] = result.ErrorMessage;
                return RedirectToAction("OrderFail");
            }

            TempData["Message"] = "Thanh toán VNPay thành công";
            return RedirectToAction("PaymentSuccess");
        }

        [Route("PaymentSuccess")]
        public IActionResult PaymentSuccess()
        {
            return View();
        }

        [Route("PaymentFail")]
        public IActionResult PaymentFail()
        {
            return View();
        }

        [Route("OrderSuccess")]
        public IActionResult OrderSuccess()
        {
            return View();
        }

        [Route("OrderFail")]
        public IActionResult OrderFail()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost("SePayIpn")]
        public IActionResult SePayIpn()
        {
            return Ok(new
            {
                success = true,
                message = "Đã nhận thông báo IPN từ SePay.",
                ipnUrl = _sePayService.GetIpnUrl()
            });
        }

        [HttpGet("CheckVoucher")]
        public async Task<JsonResult> CheckVoucher(string code, CancellationToken cancellationToken)
        {
            var result = await _clientPaymentService.CheckVoucherAsync(code, cancellationToken);
            return Json(new
            {
                success = result.Success,
                message = result.Message,
                voucher = result.Voucher
            });
        }

        private int? GetCurrentUserId()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return int.TryParse(userId, out var parsedUserId) ? parsedUserId : null;
        }
    }
}
