using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using reCAPTCHA.AspNetCore;
using System.Security.Claims;
using Tech_Store.Events;
using Tech_Store.Helper;
using Tech_Store.Helpers;
using Tech_Store.Models;
using Tech_Store.Models.DTO.Authentication;
using Tech_Store.Services;
using Tech_Store.Services.Admin.NotificationServices;

namespace Tech_Store.Controllers
{
    [Route("auth")]
    public class AuthenticationController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IRecaptchaService _recaptchaService;
        private readonly NotificationService _notificationService;
        private const int OTP_EXPIRATION_MINUTES = 3;

        public AuthenticationController(
            ApplicationDbContext context,
            IEmailService emailService,
            IRecaptchaService recaptchaService,
            NotificationService notificationService)
        {
            _context = context;
            _emailService = emailService;
            _recaptchaService = recaptchaService;
            _notificationService = notificationService;
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
                var recaptchaResult = await _recaptchaService.Validate(Request);
                if (!recaptchaResult.success)
                {
                    ViewData["ValidateMessage"] = "Captcha không hợp lệ.";
                    return View("Login", loginDto); // Specify the view name
                }

                if (!ModelState.IsValid)
                    return View(loginDto);

                var (success, message, user) = await AuthenticateUser(loginDto.Email, loginDto.Password);
                if (!success)
                {
                    ViewData["ValidateMessage"] = message;
                    return View(loginDto);
                }

                await SignInUser(user, loginDto.Remember);
             
                return RedirectToUserDashboard(user);
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
            try
            {
                // Lấy thông tin từ Google
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (result?.Principal == null)
                {
                    ViewData["ValidateMessage"] = "Không thể xác thực tài khoản Google. Vui lòng thử lại.";
                    return View("Login");
                }

                var claims = result.Principal.Identities.FirstOrDefault()?.Claims.ToList();
                if (claims == null || !claims.Any())
                {
                    ViewData["ValidateMessage"] = "Không tìm thấy thông tin người dùng từ Google.";
                    return View("Login");
                }

                // Trích xuất thông tin
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "Unknown";
                var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "Unknown";
                
                if (string.IsNullOrEmpty(email))
                {
                    ViewData["ValidateMessage"] = "Không tìm thấy email từ Google.";
                    return View("Login");
                }

                // Kiểm tra người dùng tồn tại
                var user = await _context.Users.Include(p=>p.Roles).FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Người dùng chưa tồn tại -> tạo mới
                    var passwordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()); // Mật khẩu ngẫu nhiên
                    user = new User
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        PasswordHash = passwordHash,
                        IsVerified = true,
                        IsActive = true,
                        Img = "none.png"
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Thiết lập vai trò và giỏ hàng
                    await SetupUserRole(user.UserId);
                    await CreateCart(user);
                    await _notificationService.NotifyAsync(NotificationTarget.Admins, "Người dùng mới ", $"Người dùng {user.Email} vừa tạo tài khoản !", "new register", $"/admin/users/{user.UserId}");

                }

                // Tự động đăng nhập
                await SignInUser(user, true);
                HttpContext.Session.SetString("UserEmail", email);

                return RedirectToUserDashboard(user);
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết
                // _logger.LogError(ex, "Lỗi trong quá trình xử lý Google login");
                ViewData["ValidateMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return View("Login");
            }
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
            try
            {
                // Lấy thông tin từ Google
                var result = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                if (result?.Principal == null)
                {
                    ViewData["ValidateMessage"] = "Không thể xác thực tài khoản Google. Vui lòng thử lại.";
                    return View("Login");
                }

                var claims = result.Principal.Identities.FirstOrDefault()?.Claims.ToList();
                if (claims == null || !claims.Any())
                {
                    ViewData["ValidateMessage"] = "Không tìm thấy thông tin người dùng từ Facebook.";
                    return View("Login");
                }

                // Trích xuất thông tin
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var firstName = claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value ?? "Unknown";
                var lastName = claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value ?? "Unknown";

                if (string.IsNullOrEmpty(email))
                {
                    ViewData["ValidateMessage"] = "Không tìm thấy email từ Facebook.";
                    return View("Login");
                }

                // Kiểm tra người dùng tồn tại
                var user = await _context.Users.Include(p=>p.Roles).FirstOrDefaultAsync(u => u.Email == email);

                if (user == null)
                {
                    // Người dùng chưa tồn tại -> tạo mới
                    var passwordHash = PasswordHelper.HashPassword(Guid.NewGuid().ToString()); // Mật khẩu ngẫu nhiên
                    user = new User
                    {
                        Email = email,
                        FirstName = firstName,
                        LastName = lastName,
                        PasswordHash = passwordHash,
                        IsVerified = true,
                        IsActive = true,
                        Img = "none.png"

                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Thiết lập vai trò và giỏ hàng
                    await SetupUserRole(user.UserId);
                    await CreateCart(user);

                    await _notificationService.NotifyAsync(NotificationTarget.Admins, "Người dùng mới", $"Người dùng{user.Email} vừa tạo tài khoản !", "new register", $"/admin/users/{user.UserId}");

                }

                // Tự động đăng nhập
                await SignInUser(user, true);
                HttpContext.Session.SetString("UserEmail", email);

                 return RedirectToUserDashboard(user);
            }
            catch (Exception ex)
            {
                // Log lỗi nếu cần thiết
                // _logger.LogError(ex, "Lỗi trong quá trình xử lý Google login");
                ViewData["ValidateMessage"] = "Đã xảy ra lỗi. Vui lòng thử lại.";
                return View("Login");
            }
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
                var recaptchaResult = await _recaptchaService.Validate(Request);
                if (!recaptchaResult.success)
                {
                    return Json(new { success = false, message = "Captcha không hợp lệ." });
                }

                if (!ModelState.IsValid)
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });

                if (await CheckExistEmail(registerDto.Email))
                    return Json(new { success = false, message = "Email đã được sử dụng." });

                if (registerDto.Password != registerDto.ConfirmPassword)
                    return Json(new { success = false, message = "Mật khẩu không khớp." });

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var user = await CreateOrUpdateUser(registerDto);
                    await SetupUserRole(user.UserId);
                    await CreateCart(user);
                    await SendVerificationEmail(user.Email, user.VerificationCode);
                    await transaction.CommitAsync();

                    // Auto login after registration
                    await SignInUser(user, true);
                    HttpContext.Session.SetString("UserEmail", registerDto.Email);

                    await _notificationService.NotifyAsync(NotificationTarget.Admins, "Người dùng mới", $"Người dùng{user.Email} vừa tạo tài khoản !", "new register", $"/admin/users/{user.UserId}");

                    return Json(new
                    {
                        success = true,
                        message = "Đăng ký thành công, vui lòng kiểm tra Email để nhập mã xác thực!",
                        action = "Register"
                    });
                }
                catch
                {
                    await transaction.RollbackAsync();
                    throw;
                }
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đăng ký thất bại. Vui lòng thử lại." });
            }
        }

        [HttpGet("VerifyOTP")]
        public IActionResult VerifyOTP(string actionDirect, string? error)
        {
            ViewData["actionDirect"] = actionDirect;
            ViewBag.Error = error;
            return View();
        }

        [HttpPost("verifyOTP")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> VerifyOTP(VerifyOtpDTo verifyOtp)
        {
            try
            {
                verifyOtp.Email = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(verifyOtp.Email))
                    return Json(new { success = false, message = "Phiên làm việc đã hết hạn." });

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == verifyOtp.Email);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });

                if (!await ValidateOTP(user, verifyOtp.OtpToken))
                    return Json(new { success = false, message = "Mã xác thực OTP không hợp lệ hoặc đã hết hạn!" });

                user.IsVerified = true;
                await _context.SaveChangesAsync();

                return Json(new
                {
                    success = true,
                    message = "Mã xác thực OTP hợp lệ!",
                    action = verifyOtp.Action
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
                var email = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(email))
                    return Json(new { success = false, message = "Phiên làm việc đã hết hạn." });

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });

                await UpdateAndSendNewOTP(user);
                return Json(new { success = true, message = "Đã gửi mã OTP mới." });
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
                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == email);
                if (user == null)
                    return Json(new { success = false, message = "Email chưa được đăng ký!" });

                await UpdateAndSendNewOTP(user);
                HttpContext.Session.SetString("UserEmail", email);

                return Json(new
                {
                    success = true,
                    message = "Vui lòng kiểm tra Email để lấy mã xác thực",
                    action = "ForgotPassword"
                });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã có lỗi xảy ra, vui lòng thử lại." });
            }
        }

        [HttpGet("ChangePassword")]
        public IActionResult ChangePassword() => View();


        [HttpPost("ChangePassword")]
        public async Task<JsonResult> ChangePassword(ForgotPasswordDTo forgotPassword)
        {
            try
            {
                forgotPassword.Email = HttpContext.Session.GetString("UserEmail");
                if (string.IsNullOrEmpty(forgotPassword.Email))
                    return Json(new { success = false, message = "Phiên làm việc đã hết hạn." });

                if (forgotPassword.Password != forgotPassword.ConfirmPassword)
                    return Json(new { success = false, message = "Mật khẩu không khớp." });

                var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == forgotPassword.Email);
                if (user == null)
                    return Json(new { success = false, message = "Không tìm thấy người dùng." });

                user.PasswordHash = PasswordHelper.HashPassword(forgotPassword.Password);
                await _context.SaveChangesAsync();

                // Auto login after password change
                await SignInUser(user, true);

                await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Thay đổi mật khẩu", $"Bạn đã thay đổi mật khẩu lúc {DateTime.Now}", "info", "#", new List<int> { user.UserId });

                return Json(new { success = true, message = "Thay đổi mật khẩu thành công" });
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
        private async Task<(bool success, string message, User user)> AuthenticateUser(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(x => x.Email == email);

            if (user == null || !PasswordHelper.VerifyPassword(password, user.PasswordHash))
                return (false, "Sai tài khoản hoặc mật khẩu.", null);
            if((bool)!user.IsActive)
            {
                return (false, "Bạn đã bị chặn bởi cửa hàng", null);
            }
            return (true, string.Empty, user);
        }

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
        }

        private IActionResult RedirectToUserDashboard(User user)
        {
            var isAdmin = user.Roles.Any(r => r.RoleName == "Admin");
            return Redirect(isAdmin ? "~/Admin/Index" : "/");
        }

        private async Task<bool> CheckExistEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return await _context.Users.AnyAsync(x => x.Email == email.Trim() && x.PasswordHash != null);
        }

        private async Task<User> CreateOrUpdateUser(RegisterDTo registerDto)
        {
            var verificationCode = GenerateOTP();
            var user = await _context.Users.FirstOrDefaultAsync(x => x.Email == registerDto.Email);

            if (user != null)
            {
                UpdateExistingUser(user, registerDto, verificationCode);
            }
            else
            {
                user = CreateNewUser(registerDto, verificationCode);
                _context.Users.Add(user);
            }

            await _context.SaveChangesAsync();
            return user;
        }

        private void UpdateExistingUser(User user, RegisterDTo registerDto, string verificationCode)
        {
            user.PasswordHash = PasswordHelper.HashPassword(registerDto.Password);
            user.VerificationCode = verificationCode;
            user.IsVerified = false;
            user.CreatedVerify = DateTime.Now;
            user.LastLogin = DateTime.Now;
        }

        private User CreateNewUser(RegisterDTo registerDto, string verificationCode)
        {
            return new User
            {
                Email = registerDto.Email,
                PasswordHash = PasswordHelper.HashPassword(registerDto.Password),
                VerificationCode = verificationCode,
                IsVerified = false,
                CreatedVerify = DateTime.Now,
                IsActive = true,
                Img = "none.png",
                LastLogin = DateTime.Now,
            };
        }

        private async Task SetupUserRole(int userId)
        {
            var userRole = new Dictionary<string, object>
            {
                { "UserId", userId },
                { "RoleId", 2 }
            };
            _context.Set<Dictionary<string, object>>("UserRole").Add(userRole);
            await _context.SaveChangesAsync();
        }

        private async Task SendVerificationEmail(string email, string code)
        {
            await _emailService.SendEmailAsync(new MailRequest
            {
                ToEmail = email,
                Subject = "Xác thực tài khoản",
                Body = $"Đây là mã xác thực của bạn: {code}"
            });
        }

        private string GenerateOTP()
        {
            return new Random().Next(100000, 999999).ToString();
        }

        private async Task<bool> ValidateOTP(User user, string otpToken)
        {
            if (user.VerificationCode.Trim() != otpToken.Trim())
                return false;

            var timeElapsed = DateTime.Now - user.CreatedVerify;
            return timeElapsed <= TimeSpan.FromMinutes(OTP_EXPIRATION_MINUTES);
        }

        private async Task UpdateAndSendNewOTP(User user)
        {
            user.VerificationCode = GenerateOTP();
            user.CreatedVerify = DateTime.Now;
            user.IsVerified = false;
            await _context.SaveChangesAsync();

            await _emailService.SendEmailAsync(new MailRequest
            {
                ToEmail = user.Email,
                Subject = "Mã xác thực mới",
                Body = $"Đây là mã xác thực mới của bạn: {user.VerificationCode}"
            });
        }
        private async Task CreateCart(User user)
        {
            var cart = new Cart
            {
                UserId = user.UserId,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };
            _context.Carts.Add(cart);
            await _context.SaveChangesAsync();
        }
        #endregion
    }
}