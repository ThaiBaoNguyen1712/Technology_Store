using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.DTO;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class SettingsController : BaseAdminController
    {
        public SettingsController(ApplicationDbContext context) : base(context) { }

        [Route("Index")]
        [Route("")]
        public IActionResult Index()
        {
            var settings = _context.Settings.ToList();

            // Tạo một DTO hoặc ViewModel chứa các giá trị cần thiết
            var settingsReturn = new SettingDTo
            {
                LogoUrl = settings.FirstOrDefault(s => s.Key == "LogoUrl")?.Value ?? "",
                NameWebsite = settings.FirstOrDefault(s => s.Key == "NameWebsite")?.Value ?? "",
                Slogan = settings.FirstOrDefault(s => s.Key == "Slogan")?.Value ?? "",
                Description = settings.FirstOrDefault(s => s.Key == "Description")?.Value ?? "",
                FacebookUrl = settings.FirstOrDefault(s => s.Key == "FacebookUrl")?.Value ?? "",
                InstagramUrl = settings.FirstOrDefault(s => s.Key == "InstagramUrl")?.Value ?? "",
                TwitterUrl = settings.FirstOrDefault(s => s.Key == "TwitterUrl")?.Value ?? "",
                YouTubeUrl = settings.FirstOrDefault(s => s.Key == "YoutubeUrl")?.Value ?? "",
                NameCompany = settings.FirstOrDefault(s => s.Key == "NameCompany")?.Value ?? "",
                PhoneNumber = settings.FirstOrDefault(s => s.Key == "PhoneNumber")?.Value ?? "",
                Email = settings.FirstOrDefault(s => s.Key == "Email")?.Value ?? "",
                Address = settings.FirstOrDefault(s => s.Key == "Address")?.Value ?? "",
                MoreInfo = settings.FirstOrDefault(s => s.Key == "MoreInfo")?.Value ?? ""
            };

            // Truyền dữ liệu vào View
            return View(settingsReturn);
        }


        [HttpPost("UpdateSettings")]
        public async Task<IActionResult> UpdateSettings([FromForm] SettingDTo setting, IFormFile? LogoImage)
        {
            if (setting == null)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
            }

            try
            {
                var settings = await _context.Settings.ToListAsync();

                // Lưu hình ảnh mới nếu có
                if (LogoImage != null && LogoImage.Length > 0)
                {
                    // Kiểm tra và xóa ảnh cũ nếu có
                    var oldLogoSetting = settings.FirstOrDefault(s => s.Key == "LogoUrl");
                    if (oldLogoSetting != null && !string.IsNullOrEmpty(oldLogoSetting.Value))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", oldLogoSetting.Value);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    // Lưu hình ảnh mới
                    var fileName = $"Logo_{Guid.NewGuid()}.png";
                    var uploadPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Logo");
                    Directory.CreateDirectory(uploadPath); // Đảm bảo thư mục tồn tại

                    var filePath = Path.Combine(uploadPath, fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await LogoImage.CopyToAsync(stream);
                    }

                    // Cập nhật đường dẫn mới vào cơ sở dữ liệu
                    setting.LogoUrl = Path.Combine("/","Upload", "Logo", fileName).Replace("\\", "/");
                }

                // Cập nhật từng giá trị cài đặt nếu tồn tại
                if (settings != null && settings.Any())
                {
                    settings.FirstOrDefault(s => s.Key == "LogoUrl")!.Value = setting.LogoUrl ?? "";
                    settings.FirstOrDefault(s => s.Key == "NameWebsite")!.Value = setting.NameWebsite ?? "";
                    settings.FirstOrDefault(s => s.Key == "Slogan")!.Value = setting.Slogan ?? "";
                    settings.FirstOrDefault(s => s.Key == "Description")!.Value = setting.Description ?? "";
                    settings.FirstOrDefault(s => s.Key == "FacebookUrl")!.Value = setting.FacebookUrl ?? "";
                    settings.FirstOrDefault(s => s.Key == "InstagramUrl")!.Value = setting.InstagramUrl ?? "";
                    settings.FirstOrDefault(s => s.Key == "TwitterUrl")!.Value = setting.TwitterUrl ?? "";
                    settings.FirstOrDefault(s => s.Key == "YoutubeUrl")!.Value = setting.YouTubeUrl ?? "";
                    settings.FirstOrDefault(s => s.Key == "NameCompany")!.Value = setting.NameCompany ?? "";
                    settings.FirstOrDefault(s => s.Key == "PhoneNumber")!.Value = setting.PhoneNumber ?? "";
                    settings.FirstOrDefault(s => s.Key == "Email")!.Value = setting.Email ?? "";
                    settings.FirstOrDefault(s => s.Key == "Address")!.Value = setting.Address ?? "";
                    settings.FirstOrDefault(s => s.Key == "MoreInfo")!.Value = setting.MoreInfo ?? "";

                    // Lưu các thay đổi vào cơ sở dữ liệu
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, message = "Cập nhật thành công." });
                }

                return Json(new { success = false, message = "Không tìm thấy cài đặt để cập nhật." });
            }
            catch (Exception ex)
            {
                // Xử lý lỗi
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }


    }
}
