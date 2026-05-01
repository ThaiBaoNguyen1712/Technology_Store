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
        private readonly IConfiguration _configuration;

        public SettingsController(ApplicationDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
        }

        private const int FallbackPageSize = 20;

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            return pageSize.GetValueOrDefault(FallbackPageSize) > 0 ? pageSize.Value : FallbackPageSize;
        }

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

        [HttpGet("ProductSpecs")]
        public IActionResult ProductSpecs(string? keyword, int page = 1, int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var query = _context.Species
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    x.Name.Contains(normalizedKeyword) ||
                    x.Code.Contains(normalizedKeyword) ||
                    (x.GroupName != null && x.GroupName.Contains(normalizedKeyword)));
            }

            var totalItems = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)resolvedPageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            var specs = query
                .OrderBy(x => x.GroupName)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .ToList();

            ViewBag.Keyword = keyword;
            ViewBag.Page = page;
            ViewBag.PageSize = resolvedPageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = totalItems;

            return View(specs);
        }

        [HttpGet("ProductAttributes")]
        public IActionResult ProductAttributes()
        {
            ViewBag.PageSize = GetDefaultAdminPageSize();
            return View();
        }

        [HttpGet("GetProductSpecs")]
        public IActionResult GetProductSpecs(string? keyword, int page = 1, int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var query = _context.Species
                .AsNoTracking()
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    x.Name.Contains(normalizedKeyword) ||
                    x.Code.Contains(normalizedKeyword) ||
                    (x.GroupName != null && x.GroupName.Contains(normalizedKeyword)));
            }

            var totalItems = query.Count();
            var totalPages = Math.Max(1, (int)Math.Ceiling(totalItems / (double)resolvedPageSize));
            page = Math.Max(1, Math.Min(page, totalPages));

            var specs = query
                .OrderBy(x => x.GroupName)
                .ThenBy(x => x.SortOrder)
                .ThenBy(x => x.Name)
                .Skip((page - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .Select(x => new
                {
                    x.SpecId,
                    x.Name,
                    x.Code,
                    x.GroupName,
                    x.Unit,
                    x.Description,
                    x.InputType,
                    x.SortOrder,
                    x.IsActive,
                    x.IsFilterable,
                    x.IsVisibleOnProductPage
                })
                .ToList();

            return Json(new
            {
                success = true,
                data = specs,
                pagination = new
                {
                    page,
                    pageSize = resolvedPageSize,
                    totalItems,
                    totalPages
                }
            });
        }

        [HttpGet("GetNextProductSpecMetadata")]
        public IActionResult GetNextProductSpecMetadata()
        {
            var nextSortOrder = (_context.Species.Max(x => (int?)x.SortOrder) ?? 0) + 1;
            var nextCodeNumber = (_context.Species.Count() + 1).ToString("D3");

            return Json(new
            {
                success = true,
                code = $"spec_{nextCodeNumber}",
                sortOrder = nextSortOrder
            });
        }

        [HttpPost("UpsertProductSpec")]
        public async Task<IActionResult> UpsertProductSpec([FromForm] SpecDefinitionDTo dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Name) || string.IsNullOrWhiteSpace(dto.Code))
            {
                return Json(new { success = false, message = "Tên và mã thông số là bắt buộc." });
            }

            var normalizedCode = dto.Code.Trim().ToLowerInvariant();
            var duplicated = await _context.Species
                .AnyAsync(x => x.Code == normalizedCode && x.SpecId != dto.SpecId);

            if (duplicated)
            {
                return Json(new { success = false, message = "Mã thông số đã tồn tại." });
            }

            Specs spec;
            if (dto.SpecId.HasValue && dto.SpecId.Value > 0)
            {
                spec = await _context.Species.FirstOrDefaultAsync(x => x.SpecId == dto.SpecId.Value) ?? new Specs();
                if (spec.SpecId == 0)
                {
                    return Json(new { success = false, message = "Không tìm thấy thông số cần cập nhật." });
                }
            }
            else
            {
                spec = new Specs();
                _context.Species.Add(spec);
            }

            spec.Name = dto.Name.Trim();
            spec.Code = normalizedCode;
            spec.GroupName = string.IsNullOrWhiteSpace(dto.GroupName) ? "Thông số chung" : dto.GroupName.Trim();
            spec.Unit = string.IsNullOrWhiteSpace(dto.Unit) ? null : dto.Unit.Trim();
            spec.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            spec.InputType = string.IsNullOrWhiteSpace(dto.InputType) ? "text" : dto.InputType.Trim().ToLowerInvariant();
            spec.SortOrder = dto.SortOrder;
            spec.IsActive = dto.IsActive;
            spec.IsFilterable = dto.IsFilterable;
            spec.IsVisibleOnProductPage = dto.IsVisibleOnProductPage;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = dto.SpecId.HasValue ? "Đã cập nhật thông số." : "Đã thêm thông số." });
        }

        [HttpPost("DeleteProductSpec")]
        public async Task<IActionResult> DeleteProductSpec(int specId)
        {
            var spec = await _context.Species
                .Include(x => x.SpecValues)
                .FirstOrDefaultAsync(x => x.SpecId == specId);

            if (spec == null)
            {
                return Json(new { success = false, message = "Không tìm thấy thông số." });
            }

            if (spec.SpecValues.Any())
            {
                spec.IsActive = false;
                await _context.SaveChangesAsync();
                return Json(new { success = true, message = "Thông số đang được sử dụng nên đã được chuyển sang trạng thái ngừng hoạt động." });
            }

            _context.Species.Remove(spec);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa thông số." });
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
                    var logoSetting = settings.FirstOrDefault(s => s.Key == "LogoUrl");
                    if (!string.IsNullOrEmpty(setting.LogoUrl))
                    {
                        logoSetting!.Value = setting.LogoUrl;
                    }

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
