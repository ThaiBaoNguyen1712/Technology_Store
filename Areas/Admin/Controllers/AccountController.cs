using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using Tech_Store.Helper;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.DTO.Authentication;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class AccountController : BaseAdminController
    {

        public AccountController(ApplicationDbContext context) :  base(context) { }

        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user_Infor = await _context.Users.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
            var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
            if (user_Infor == null)
                return NotFound();

            if (address != null && !string.IsNullOrEmpty(address.AddressLine))
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
            var usDto = new UserDTo
            {
                UserId = user_Infor.UserId,
                FirstName = user_Infor.FirstName,
                LastName = user_Infor.LastName,
                Email = user_Infor.Email,
                PhoneNumber = user_Infor.PhoneNumber,
                ImageUrl = user_Infor.Img,
                //Address
                AddressLine = address?.AddressLine ?? "",
                Ward = address?.Ward ?? "",
                District = address?.District ?? "",
                Province = address?.Province ?? "",

            };
            return View(usDto);
        }
        [HttpPost("ChangeAddress")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ChangeAddress(string addressLine, string ward, string province, string district)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return Json(new { success = false, message = "Thông tin không hợp lệ." });
                }
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Kiểm tra xem userId có hợp lệ không
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    return Json(new { success = false, message = "Người dùng không hợp lệ." });
                }

                var address_get = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == parsedUserId);

                if (address_get != null)
                {
                    // Cập nhật địa chỉ
                    address_get.AddressLine = addressLine;
                    address_get.Ward = ward;
                    address_get.District = district;
                    address_get.Province = province;
                }
                else
                {
                    // Thêm địa chỉ mới
                    var address_add = new Address
                    {
                        UserId = parsedUserId, // Thêm UserId vào địa chỉ mới
                        AddressLine = addressLine,
                        Ward = ward,
                        District = district,
                        Province = province
                    };
                    _context.Addresses.Add(address_add);
                }

                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thay đổi địa chỉ thành công" });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi và trả về thông báo
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        [HttpPost("ChangePersonalInfo")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePersonalInfo(string LastName, string FirstName, string Email, string PhoneNumber)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // Kiểm tra xem userId có hợp lệ không
                if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
                {
                    return Json(new { success = false, message = "Người dùng không hợp lệ." });
                }
                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
                user.LastName = LastName;
                user.FirstName = FirstName;
                user.Email = Email;
                user.PhoneNumber = PhoneNumber;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thay đổi thông tin thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
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
            if (!PasswordHelper.VerifyPassword(changePasswordDTo.OldPassword, user_get.PasswordHash))
            {
                return Json(new { success = false, message = "Mật khẩu cũ không đúng" });
            }

            // Băm mật khẩu mới và lưu lại
            var newPassword = PasswordHelper.HashPassword(changePasswordDTo.Password);
            user_get.PasswordHash = newPassword;
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Thay đổi mật khẩu thành công" });
        }
        [Route("logout")]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }
    }
}
