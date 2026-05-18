using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Events;
using Tech_Store.Helper;
using Tech_Store.Helpers;
using Tech_Store.Models;
using Tech_Store.Models.DTO.Authentication;
using Tech_Store.Models.Enums;
using Tech_Store.Services;
using Tech_Store.Services.Admin.NotificationServices;
using Tech_Store.Services.Auth;
using Tech_Store.Services.Interfaces.Auth;
using Tech_Store.Services.Recommendation;

namespace Tech_Store.Controllers
{
    [Route("auth")]
    public class AuthenticationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IAuthenticationFlowService _authenticationFlowService;
        private readonly ICloudflareTurnstileService _cloudflareTurnstileService;
        private readonly NotificationService _notificationService;
        private readonly IUserProductEventTrackingService _userProductEventTrackingService;
        private readonly IUserActivityTrackingService _userActivityTrackingService;
        private readonly IExternalAuthenticationService _externalAuthenticationService;

        public AuthenticationController(
            ApplicationDbContext context,
            IAuthenticationFlowService authenticationFlowService,
            ICloudflareTurnstileService cloudflareTurnstileService,
            NotificationService notificationService,
            IUserProductEventTrackingService userProductEventTrackingService,
            IUserActivityTrackingService userActivityTrackingService,
            IExternalAuthenticationService externalAuthenticationService)
        {
            _context = context;
            _authenticationFlowService = authenticationFlowService;
            _cloudflareTurnstileService = cloudflareTurnstileService;
            _notificationService = notificationService;
            _userProductEventTrackingService = userProductEventTrackingService;
            _userActivityTrackingService = userActivityTrackingService;
            _externalAuthenticationService = externalAuthenticationService;
        }

        #region Public Actions
        [HttpGet("login")]
        public IActionResult Login()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDTo loginDto)
        {
            try
            {
                // Kiểm tra Captcha
                var captchaResult = await _cloudflareTurnstileService.ValidateAsync(Request);
                if (!captchaResult.IsSuccess)
                {
                    ViewData["ValidateMessage"] = captchaResult.ErrorMessage;
                    return View("Login", loginDto); // Specify the view name
                }

                if (!ModelState.IsValid)
                    return View(loginDto);

                var loginResult = await _authenticationFlowService.AuthenticateUserAsync(loginDto.Email, loginDto.Password);
                if (!loginResult.IsSuccess || loginResult.User == null)
                {
                    ViewData["ValidateMessage"] = loginResult.Message;
                    return View(loginDto);
                }

                var guestId = HttpContext.Items["guest_id"]?.ToString() ?? HttpContext.Request.Cookies["guest_id"];

                if (!string.IsNullOrEmpty(guestId))
                {
                    await _userProductEventTrackingService.MergeSessionToUserAsync(guestId, loginResult.User.UserId);
                }

                await SignInUser(loginResult.User, loginDto.Remember);
             
                return RedirectToUserDashboard(loginResult.User);
            }
            catch (Exception)
            {
                ViewData["ValidateMessage"] = "Đã xảy ra lỗi trong quá trình đăng nhập.";
                return View(loginDto);
            }
        }
        [Route("LoginByGoogle")]
        public async Task LoginByGoogle()
        {
            await HttpContext.ChallengeAsync(GoogleDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = Url.Action("GoogleResponse")
            });
        }
        [Route("GoogleResponse")]
        public async Task<IActionResult> GoogleResponse()
        {
            return await HandleExternalLoginResponseAsync("Google");
        }
        [Route("LoginByFacebook")]
        public async Task LoginByFacebook()
        {
            await HttpContext.ChallengeAsync(FacebookDefaults.AuthenticationScheme, new AuthenticationProperties
            {
                RedirectUri = Url.Action("FacebookResponse")
            });
        }
        [Route("FacebookResponse")]
        public async Task<IActionResult> FacebookResponse()
        {
            return await HandleExternalLoginResponseAsync("Facebook");
        }
        [HttpGet("Register")]
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost("register")]
        public async Task<JsonResult> Register(RegisterDTo registerDto)
        {
            try
            {
                // Kiểm tra Captcha
                var captchaResult = await _cloudflareTurnstileService.ValidateAsync(Request);
                if (!captchaResult.IsSuccess)
                {
                    return Json(new { success = false, message = captchaResult.ErrorMessage });
                }

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

                var otpResult = await _authenticationFlowService.RegisterAsync(registerDto, HttpContext.Session);
                if (!otpResult.IsSuccess)
                {
                    return Json(new
                    {
                        success = false,
                        message = otpResult.Message,
                        resendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                    });
                }

                return Json(new
                {
                    success = true,
                    message = otpResult.Message,
                    action = otpResult.ActionType.ToRouteValue(),
                    otpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                    resendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đăng ký thất bại. Vui lòng thử lại." });
            }
        }

        [HttpGet("VerifyOTP")]
        public IActionResult VerifyOTP(string actionDirect, string? error)
        {
            var otpPageState = _authenticationFlowService.GetOtpPageState(HttpContext.Session);
            if (!otpPageState.IsSuccess)
            {
                return RedirectToAction("Login");
            }

            ViewData["actionDirect"] = otpPageState.ActionType.ToRouteValue();
            ViewBag.Error = error;
            ViewData["OtpExpiresInSeconds"] = otpPageState.OtpExpiresInSeconds;
            ViewData["ResendAvailableInSeconds"] = otpPageState.ResendAvailableInSeconds;
            return View();
        }

        [HttpPost("verifyOTP")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> VerifyOTP(VerifyOtpDTo verifyOtp)
        {
            try
            {
                var otpResult = await _authenticationFlowService.VerifyOtpAsync(verifyOtp, HttpContext.Session);
                if (!otpResult.IsSuccess)
                {
                    return Json(new
                    {
                        success = false,
                        message = otpResult.Message,
                        otpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                        resendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                    });
                }

                if (otpResult.ActionType == AuthOtpActionType.Register)
                {
                    var user = await _context.Users
                        .Include(x => x.Roles)
                        .FirstOrDefaultAsync(x => x.Email == otpResult.Email);

                    if (user != null)
                    {
                        await SignInUser(user, true);
                    }
                }

                return Json(new
                {
                    success = true,
                    message = "Mã xác thực OTP hợp lệ!",
                    action = otpResult.ActionType.ToRouteValue()
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi xác thực OTP." });
            }
        }

        [HttpPost("ResendOTP")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ResendOTP()
        {
            try
            {
                var otpResult = await _authenticationFlowService.ResendOtpAsync(HttpContext.Session);
                if (!otpResult.IsSuccess)
                {
                    return Json(new
                    {
                        success = false,
                        message = otpResult.Message,
                        otpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                        resendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                    });
                }

                return Json(new
                {
                    success = true,
                    message = otpResult.Message,
                    otpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                    resendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi gửi lại OTP." });
            }
        }

        [HttpGet("forgot-password")]
        public IActionResult ForgotPassword() {
            if (User.Identity.IsAuthenticated)
                return RedirectToAction("Index", "Home");
            return View();
        }

        [HttpPost("forgot-password")]
        public async Task<JsonResult> ForgotPassword(string email)
        {
            try
            {
                var otpResult = await _authenticationFlowService.ForgotPasswordAsync(email, HttpContext.Session);
                if (!otpResult.IsSuccess)
                {
                    return Json(new
                    {
                        success = false,
                        message = otpResult.Message,
                        resendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                    });
                }

                return Json(new
                {
                    success = true,
                    message = otpResult.Message,
                    action = otpResult.ActionType.ToRouteValue(),
                    otpExpiresInSeconds = otpResult.OtpExpiresInSeconds,
                    resendAvailableInSeconds = otpResult.ResendAvailableInSeconds
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra, vui lòng thử lại." });
            }
        }

        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword()
        {
            return _authenticationFlowService.CanAccessChangePassword(HttpContext.Session)
                ? View()
                : RedirectToAction("ForgotPassword");
        }


        [HttpPost("ChangePassword")]
        public async Task<JsonResult> ChangePassword(ForgotPasswordDTo forgotPassword)
        {
            try
            {
                var result = await _authenticationFlowService.ChangeForgottenPasswordAsync(forgotPassword, HttpContext.Session);
                return Json(new { success = result.IsSuccess, message = result.Message });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi khi đổi mật khẩu." });
            }
        }

        [HttpPost("ChangePassword2")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ChangePassword2(ChangePasswordDTo changePasswordDTo)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Json(new { success = false, message = "Đã có lỗi xác thực" });
            }

            if (!changePasswordDTo.Password.Equals(changePasswordDTo.ConfirmPassword))
            {
                return Json(new { success = false, message = "Mật khẩu không trùng khớp" });
            }

            var user_get = await _context.Users.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId) && x.Email.Trim() == changePasswordDTo.Email.Trim());
            if (user_get == null)
            {
                return Json(new { success = false, message = "Người dùng không tồn tại" });
            }

            // Sử dụng phương thức xác minh của PasswordHelper
            if (!PasswordHelper.VerifyPassword( changePasswordDTo.OldPassword, user_get.PasswordHash))
            {
                return Json(new { success = false, message = "Mật khẩu cũ không đúng" });
            }

            // Băm mật khẩu mới và lưu lại
            var newPassword = PasswordHelper.HashPassword(changePasswordDTo.Password);
            user_get.PasswordHash = newPassword;
            await _context.SaveChangesAsync();

            await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Thay đổi mật khẩu", $"Bạn đã thay đổi mật khẩu lúc {DateTime.Now}", "info", "#", new List<int> { user_get.UserId });


            return Json(new { success = true, message = "Thay đổi mật khẩu thành công" });
        }

        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            // Xóa toàn bộ thông tin xác thực
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Xóa Session
            HttpContext.Session.Clear();

            // Xóa Claims của User (dành cho Claims-based Authentication)
            HttpContext.User = new ClaimsPrincipal(new ClaimsIdentity());

            return RedirectToAction("Index", "Home");
        }

        #endregion

        #region Private Helper Methods
        private async Task SignInUser(User user, bool isPersistent)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()), // ID người dùng
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Roles.FirstOrDefault()?.RoleName ?? "Customer")
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                AllowRefresh = true,
                IsPersistent = isPersistent
            };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

            await _userActivityTrackingService.TrackLoginAsync(user.UserId, HttpContext);
        }

        private async Task<IActionResult> HandleExternalLoginResponseAsync(string providerName)
        {
            try
            {
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (result?.Principal == null)
                {
                    ViewData["ValidateMessage"] = $"Không thể xác thực tài khoản {providerName}. Vui lòng thử lại.";
                    return View("Login");
                }

                var authResult = await _externalAuthenticationService.GetOrCreateUserAsync(result.Principal, providerName);
                if (!authResult.IsSuccess || authResult.User == null)
                {
                    ViewData["ValidateMessage"] = authResult.ErrorMessage ?? "Đã xảy ra lỗi. Vui lòng thử lại.";
                    return View("Login");
                }

                await SignInUser(authResult.User, true);
                HttpContext.Session.SetString("UserEmail", authResult.User.Email);

                return RedirectToUserDashboard(authResult.User);
            }
            catch
            {
                ViewData["ValidateMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return View("Login");
            }
        }

        private IActionResult RedirectToUserDashboard(User user)
        {
            var isAdmin = user.Roles.Any(r => r.RoleName == "Admin");
            return Redirect(isAdmin ? "~/Admin/Index" : "/");
        }

        #endregion
    }
}
