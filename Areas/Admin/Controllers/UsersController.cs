using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class UsersController : BaseAdminController
    {
        private readonly IConfiguration _configuration;
        private const int FallbackPageSize = 20;

        public UsersController(ApplicationDbContext context, IConfiguration configuration) : base(context)
        {
            _configuration = configuration;
        }

        private sealed class AdminUserQueryItem
        {
            public int UserId { get; set; }
            public string? LastName { get; set; }
            public string? FirstName { get; set; }
            public string? PhoneNumber { get; set; }
            public string Email { get; set; } = string.Empty;
            public string? ImageUrl { get; set; }
            public bool IsActive { get; set; }
            public bool IsVerified { get; set; }
            public DateTime? CreatedAt { get; set; }
            public DateTime? LastLogin { get; set; }
            public string? LastLoginIp { get; set; }
            public string? LastLoginDevice { get; set; }
            public DateTime? LastRequestAt { get; set; }
            public string? LastRequestIp { get; set; }
            public int OrderCount { get; set; }
            public decimal TotalSpent { get; set; }
        }

        private int GetDefaultAdminPageSize()
        {
            var pageSize = _configuration.GetValue<int?>("AdminUi:DefaultPageSize");
            var resolvedPageSize = pageSize.GetValueOrDefault(FallbackPageSize);
            return resolvedPageSize > 0 ? resolvedPageSize : FallbackPageSize;
        }

        private IQueryable<AdminUserQueryItem> BuildUserQuery()
        {
            return _context.Users
                .AsNoTracking()
                .Select(x => new AdminUserQueryItem
                {
                    UserId = x.UserId,
                    LastName = x.LastName,
                    FirstName = x.FirstName,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,
                    ImageUrl = x.Img,
                    IsActive = x.IsActive ?? false,
                    IsVerified = x.IsVerified ?? false,
                    CreatedAt = x.CreatedAt,
                    LastLogin = x.LastLogin,
                    LastLoginIp = x.LastLoginIp,
                    LastLoginDevice = x.LastLoginDevice,
                    LastRequestAt = x.LastRequestAt,
                    LastRequestIp = x.LastRequestIp,
                    OrderCount = x.Orders.Count(),
                    TotalSpent = x.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0m
                });
        }

        private IQueryable<AdminUserQueryItem> ApplyUserFilters(
            IQueryable<AdminUserQueryItem> query,
            string? keyword,
            string? status,
            string? email,
            decimal? revenueFrom,
            decimal? revenueTo,
            DateTime? createdDateFrom,
            DateTime? createdDateTo,
            string? phoneNumber)
        {
            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    (x.FirstName ?? string.Empty).Contains(normalizedKeyword) ||
                    (x.LastName ?? string.Empty).Contains(normalizedKeyword) ||
                    ((x.LastName ?? string.Empty) + " " + (x.FirstName ?? string.Empty)).Contains(normalizedKeyword) ||
                    (x.Email ?? string.Empty).Contains(normalizedKeyword) ||
                    (x.PhoneNumber ?? string.Empty).Contains(normalizedKeyword));
            }

            if (!string.IsNullOrWhiteSpace(status) && bool.TryParse(status, out var isActive))
            {
                query = query.Where(x => x.IsActive == isActive);
            }

            if (!string.IsNullOrWhiteSpace(email))
            {
                query = query.Where(x => x.Email.Contains(email.Trim()));
            }

            if (!string.IsNullOrWhiteSpace(phoneNumber))
            {
                query = query.Where(x => (x.PhoneNumber ?? string.Empty).Contains(phoneNumber.Trim()));
            }

            if (revenueFrom.HasValue)
            {
                query = query.Where(x => x.TotalSpent >= revenueFrom.Value);
            }

            if (revenueTo.HasValue)
            {
                query = query.Where(x => x.TotalSpent <= revenueTo.Value);
            }

            if (createdDateFrom.HasValue)
            {
                var fromDate = createdDateFrom.Value.Date;
                query = query.Where(x => x.CreatedAt.HasValue && x.CreatedAt.Value >= fromDate);
            }

            if (createdDateTo.HasValue)
            {
                var toDateExclusive = createdDateTo.Value.Date.AddDays(1);
                query = query.Where(x => x.CreatedAt.HasValue && x.CreatedAt.Value < toDateExclusive);
            }

            return query;
        }

        private RouteValueDictionary BuildIndexRouteValues(
            int page,
            int pageSize,
            string? keyword,
            string? status,
            string? email,
            string? phoneNumber,
            decimal? revenueFrom,
            decimal? revenueTo,
            DateTime? createdDateFrom,
            DateTime? createdDateTo)
        {
            return new RouteValueDictionary
            {
                ["page"] = Math.Max(1, page),
                ["keyword"] = keyword,
                ["status"] = status,
                ["email"] = email,
                ["phoneNumber"] = phoneNumber,
                ["revenueFrom"] = revenueFrom,
                ["revenueTo"] = revenueTo,
                ["createdDateFrom"] = createdDateFrom?.ToString("yyyy-MM-dd"),
                ["createdDateTo"] = createdDateTo?.ToString("yyyy-MM-dd")
            };
        }

        private async Task<bool> PopulateUserDetailAsync(AdminUserDetailViewModel model, int id)
        {
            var detail = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == id)
                .Select(x => new
                {
                    x.UserId,
                    x.CreatedAt,
                    x.LastLogin,
                    x.LastLoginIp,
                    x.LastLoginDevice,
                    x.LastRequestAt,
                    x.LastRequestIp,
                    x.LastRequestDevice,
                    IsVerified = x.IsVerified ?? false,
                    OrderCount = x.Orders.Count(),
                    TotalSpent = x.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                    Province = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.Province).FirstOrDefault(),
                    District = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.District).FirstOrDefault(),
                    Ward = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.Ward).FirstOrDefault(),
                    AddressLine = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.AddressLine).FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (detail == null)
            {
                return false;
            }

            model.UserId = detail.UserId;
            model.OrderCount = detail.OrderCount;
            model.TotalSpent = detail.TotalSpent;
            model.CreatedAt = detail.CreatedAt;
            model.LastLogin = detail.LastLogin;
            model.LastLoginIp = detail.LastLoginIp;
            model.LastLoginDevice = detail.LastLoginDevice;
            model.LastRequestAt = detail.LastRequestAt;
            model.LastRequestIp = detail.LastRequestIp;
            model.LastRequestDevice = detail.LastRequestDevice;
            model.IsVerified = detail.IsVerified;
            model.Province ??= detail.Province;
            model.District ??= detail.District;
            model.Ward ??= detail.Ward;
            model.AddressLine ??= detail.AddressLine;

            model.RecentOrders = await _context.Orders
                .AsNoTracking()
                .Where(x => x.UserId == id)
                .OrderByDescending(x => x.OrderDate ?? x.CreatedAt)
                .Take(6)
                .Select(x => new AdminUserOrderSummaryItemViewModel
                {
                    OrderId = x.OrderId,
                    OrderStatus = x.OrderStatus,
                    TotalAmount = x.TotalAmount,
                    OrderDate = x.OrderDate
                })
                .ToListAsync();

            return true;
        }

        private static string BuildAddressDisplay(string? addressLine, string? ward, string? district, string? province)
        {
            return string.Join(", ", new[] { addressLine, ward, district, province }.Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        [HttpGet]
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(
            string? status,
            string? email,
            decimal? revenueFrom,
            decimal? revenueTo,
            DateTime? createdDateFrom,
            DateTime? createdDateTo,
            string? phoneNumber,
            int page = 1,
            int? pageSize = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            if (resolvedPageSize <= 0)
            {
                resolvedPageSize = GetDefaultAdminPageSize();
            }

            var query = ApplyUserFilters(
                BuildUserQuery(),
                null,
                status,
                email,
                revenueFrom,
                revenueTo,
                createdDateFrom,
                createdDateTo,
                phoneNumber);

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);
            var currentPage = Math.Min(Math.Max(1, page), totalPages);

            var users = await query
                .OrderByDescending(x => x.UserId)
                .Skip((currentPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .Select(x => new UserVM
                {
                    UserId = x.UserId,
                    LastName = x.LastName ?? string.Empty,
                    FirstName = x.FirstName ?? string.Empty,
                    PhoneNumber = x.PhoneNumber ?? string.Empty,
                    Email = x.Email,
                    ImageUrl = x.ImageUrl ?? "none.png",
                    IsActive = x.IsActive,
                    OrderCount = x.OrderCount,
                    TotalSpent = x.TotalSpent,
                    CreatedAt = x.CreatedAt,
                    LastLogin = x.LastLogin,
                    LastLoginIp = x.LastLoginIp ?? string.Empty,
                    LastLoginDevice = x.LastLoginDevice ?? string.Empty,
                    LastRequestAt = x.LastRequestAt,
                    LastRequestIp = x.LastRequestIp ?? string.Empty
                })
                .ToListAsync();

            var model = new AdminUserIndexViewModel
            {
                Users = users,
                Status = status,
                Email = email,
                PhoneNumber = phoneNumber,
                RevenueFrom = revenueFrom,
                RevenueTo = revenueTo,
                CreatedDateFrom = createdDateFrom,
                CreatedDateTo = createdDateTo,
                Page = currentPage,
                PageSize = resolvedPageSize,
                TotalPages = totalPages,
                TotalItems = totalItems
            };

            return View(model);
        }

        [HttpGet("Create")]
        public IActionResult Create(
            int page = 1,
            int? pageSize = null,
            string? status = null,
            string? email = null,
            string? phoneNumber = null,
            decimal? revenueFrom = null,
            decimal? revenueTo = null,
            DateTime? createdDateFrom = null,
            DateTime? createdDateTo = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var model = new AdminUserCreateViewModel
            {
                IsActive = true,
                ReturnPage = Math.Max(1, page),
                ReturnPageSize = resolvedPageSize,
                ReturnStatus = status,
                ReturnEmail = email,
                ReturnPhoneNumber = phoneNumber,
                ReturnRevenueFrom = revenueFrom,
                ReturnRevenueTo = revenueTo,
                ReturnCreatedDateFrom = createdDateFrom,
                ReturnCreatedDateTo = createdDateTo
            };

            return View(model);
        }

        [HttpGet("QuickDrawer/{id}")]
        public async Task<IActionResult> QuickDrawer(
            int id,
            int page = 1,
            int? pageSize = null,
            string? keyword = null,
            string? status = null,
            string? email = null,
            string? phoneNumber = null,
            decimal? revenueFrom = null,
            decimal? revenueTo = null,
            DateTime? createdDateFrom = null,
            DateTime? createdDateTo = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var user = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == id)
                .Select(x => new AdminUserQuickDrawerViewModel
                {
                    UserId = x.UserId,
                    FullName = ((x.LastName ?? string.Empty) + " " + (x.FirstName ?? string.Empty)).Trim(),
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,
                    ImageUrl = x.Img,
                    IsActive = x.IsActive ?? false,
                    IsVerified = x.IsVerified ?? false,
                    OrderCount = x.Orders.Count(),
                    TotalSpent = x.Orders.Sum(o => (decimal?)o.TotalAmount) ?? 0m,
                    CreatedAt = x.CreatedAt,
                    LastLogin = x.LastLogin,
                    LastLoginIp = x.LastLoginIp,
                    LastLoginDevice = x.LastLoginDevice,
                    LastRequestAt = x.LastRequestAt,
                    LastRequestIp = x.LastRequestIp,
                    LastRequestDevice = x.LastRequestDevice,
                    AddressLine = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.AddressLine).FirstOrDefault(),
                    Ward = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.Ward).FirstOrDefault(),
                    District = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.District).FirstOrDefault(),
                    Province = x.Addresses.OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt).Select(a => a.Province).FirstOrDefault()
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound();
            }

            user.Address = BuildAddressDisplay(user.AddressLine, user.Ward, user.District, user.Province);
            user.DetailUrl = Url.Action(
                "ViewUser",
                "Users",
                new
                {
                    area = "Admin",
                    id,
                    page,
                    pageSize = resolvedPageSize,
                    keyword,
                    status,
                    email,
                    phoneNumber,
                    revenueFrom,
                    revenueTo,
                    createdDateFrom = createdDateFrom?.ToString("yyyy-MM-dd"),
                    createdDateTo = createdDateTo?.ToString("yyyy-MM-dd")
                }) ?? $"/Admin/Users/View/{id}";

            return PartialView("_QuickDrawerContent", user);
        }

        [HttpGet("View/{id}", Name = "AdminUserDetail")]
        public async Task<IActionResult> ViewUser(
            int id,
            int page = 1,
            int? pageSize = null,
            string? keyword = null,
            string? status = null,
            string? email = null,
            string? phoneNumber = null,
            decimal? revenueFrom = null,
            decimal? revenueTo = null,
            DateTime? createdDateFrom = null,
            DateTime? createdDateTo = null)
        {
            var resolvedPageSize = pageSize.GetValueOrDefault(GetDefaultAdminPageSize());
            var model = await _context.Users
                .AsNoTracking()
                .Where(x => x.UserId == id)
                .Select(x => new AdminUserDetailViewModel
                {
                    UserId = x.UserId,
                    FirstName = x.FirstName ?? string.Empty,
                    LastName = x.LastName ?? string.Empty,
                    PhoneNumber = x.PhoneNumber ?? string.Empty,
                    Email = x.Email,
                    IsActive = x.IsActive ?? false,
                    ImageUrl = x.Img
                })
                .FirstOrDefaultAsync();

            if (model == null)
            {
                return NotFound();
            }

            if (!await PopulateUserDetailAsync(model, id))
            {
                return NotFound();
            }

            model.ReturnPage = Math.Max(1, page);
            model.ReturnPageSize = resolvedPageSize;
            model.ReturnKeyword = keyword;
            model.ReturnStatus = status;
            model.ReturnEmail = email;
            model.ReturnPhoneNumber = phoneNumber;
            model.ReturnRevenueFrom = revenueFrom;
            model.ReturnRevenueTo = revenueTo;
            model.ReturnCreatedDateFrom = createdDateFrom;
            model.ReturnCreatedDateTo = createdDateTo;

            return View("View", model);
        }

        [AutoValidateAntiforgeryToken]
        [HttpPost("Create")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> Create([FromForm] AdminUserCreateViewModel us)
        {
            if (!CheckEmailExist(us.Email))
            {
                ModelState.AddModelError(nameof(us.Email), "Email đã tồn tại.");
            }
            if (!CheckPhoneNumberExist(us.PhoneNumber))
            {
                ModelState.AddModelError(nameof(us.PhoneNumber), "Số điện thoại đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                return View("Create", us);
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var user = new User
                {
                    FirstName = us.FirstName,
                    LastName = us.LastName,
                    Email = us.Email,
                    PhoneNumber = us.PhoneNumber,
                    IsActive = us.IsActive,
                };

                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                if (us.Image != null && us.Image.Length > 0)
                {
                    var fileName = $"KH_{Guid.NewGuid()}.png";
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);

                    await using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await us.Image.CopyToAsync(stream);
                    }

                    user.Img = fileName;
                }
                else
                {
                    user.Img = "none.jpg";
                }

                var userRole = new Dictionary<string, object>
                {
                    { "UserId", user.UserId },
                    { "RoleId", 2 }
                };

                _context.Set<Dictionary<string, object>>("UserRole").Add(userRole);

                var address = new Address
                {
                    UserId = user.UserId,
                    Ward = us.Ward,
                    District = us.District,
                    Province = us.Province,
                    AddressLine = us.AddressLine,
                };
                _context.Addresses.Add(address);

                var cart = new Cart
                {
                    UserId = user.UserId,
                };
                _context.Carts.Add(cart);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                TempData["UserAdminSuccess"] = "Đã thêm khách hàng mới.";

                return RedirectToAction(
                    nameof(Index),
                    BuildIndexRouteValues(
                        us.ReturnPage,
                        us.ReturnPageSize,
                        null,
                        us.ReturnStatus,
                        us.ReturnEmail,
                        us.ReturnPhoneNumber,
                        us.ReturnRevenueFrom,
                        us.ReturnRevenueTo,
                        us.ReturnCreatedDateFrom,
                        us.ReturnCreatedDateTo));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                ModelState.AddModelError(string.Empty, $"Đã xảy ra lỗi khi tạo khách hàng: {ex.Message}");
                return View("Create", us);
            }
        }

        [HttpPost("Save/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveDetail(int id, AdminUserDetailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateUserDetailAsync(model, id);
                return View("View", model);
            }

            if (!CheckPhoneNumberExistForUpdate(model.PhoneNumber, id))
            {
                ModelState.AddModelError(nameof(model.PhoneNumber), "Số điện thoại đã tồn tại.");
                await PopulateUserDetailAsync(model, id);
                return View("View", model);
            }

            if (!CheckEmailExistForUpdate(model.Email, id))
            {
                ModelState.AddModelError(nameof(model.Email), "Email đã tồn tại.");
                await PopulateUserDetailAsync(model, id);
                return View("View", model);
            }

            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            user.FirstName = model.FirstName?.Trim();
            user.LastName = model.LastName?.Trim();
            user.Email = model.Email.Trim();
            user.PhoneNumber = model.PhoneNumber.Trim();
            user.IsActive = model.IsActive;
            user.UpdatedAt = DateTime.Now;

            if (model.Image != null && model.Image.Length > 0)
            {
                if (!string.IsNullOrEmpty(user.Img) && !string.Equals(user.Img, "none.png", StringComparison.OrdinalIgnoreCase) && !string.Equals(user.Img, "none.jpg", StringComparison.OrdinalIgnoreCase))
                {
                    var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", user.Img);
                    if (System.IO.File.Exists(oldImagePath))
                    {
                        System.IO.File.Delete(oldImagePath);
                    }
                }

                var fileName = $"KH_{Guid.NewGuid()}.png";
                var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", fileName);
                Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);

                await using (var stream = new FileStream(imagePath, FileMode.Create))
                {
                    await model.Image.CopyToAsync(stream);
                }

                user.Img = fileName;
            }

            var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == id);
            if (address == null)
            {
                address = new Address
                {
                    UserId = id
                };
                _context.Addresses.Add(address);
            }

            address.AddressLine = model.AddressLine;
            address.Province = model.Province;
            address.District = model.District;
            address.Ward = model.Ward;
            address.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["UserAdminSuccess"] = "Đã cập nhật thông tin khách hàng.";

            return RedirectToAction(
                nameof(ViewUser),
                BuildIndexRouteValues(
                    model.ReturnPage,
                    model.ReturnPageSize,
                    model.ReturnKeyword,
                    model.ReturnStatus,
                    model.ReturnEmail,
                    model.ReturnPhoneNumber,
                    model.ReturnRevenueFrom,
                    model.ReturnRevenueTo,
                    model.ReturnCreatedDateFrom,
                    model.ReturnCreatedDateTo)
                    .Append(new KeyValuePair<string, object?>("id", id)));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var us = await _context.Users.FindAsync(id);
            var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == id);
            if (us == null)
            {
                return NotFound();
            }

            var usDto = new UserDTo
            {
                UserId = us.UserId,
                FirstName = us.FirstName ?? string.Empty,
                LastName = us.LastName ?? string.Empty,
                Email = us.Email,
                PhoneNumber = us.PhoneNumber ?? string.Empty,
                IsActive = us.IsActive ?? false,
                ImageUrl = us.Img,
                AddressLine = address?.AddressLine ?? string.Empty,
                Ward = address?.Ward ?? string.Empty,
                District = address?.District ?? string.Empty,
                Province = address?.Province ?? string.Empty,
            };
            return Json(usDto);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UserDTo updateDto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest("Thiếu thông tin");
            }

            if (!CheckPhoneNumberExistForUpdate(updateDto.PhoneNumber, id))
            {
                return BadRequest("Số điện thoại đã tồn tại.");
            }

            if (!CheckEmailExistForUpdate(updateDto.Email, id))
            {
                return BadRequest("Email đã tồn tại.");
            }

            try
            {
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                user.FirstName = updateDto.FirstName;
                user.LastName = updateDto.LastName;
                user.Email = updateDto.Email.Trim();
                user.PhoneNumber = updateDto.PhoneNumber.Trim();
                user.IsActive = updateDto.IsActive;
                user.UpdatedAt = DateTime.Now;

                if (updateDto.Image != null && updateDto.Image.Length > 0)
                {
                    if (!string.IsNullOrEmpty(user.Img) && !string.Equals(user.Img, "none.png", StringComparison.OrdinalIgnoreCase) && !string.Equals(user.Img, "none.jpg", StringComparison.OrdinalIgnoreCase))
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", user.Img);
                        if (System.IO.File.Exists(oldImagePath))
                        {
                            System.IO.File.Delete(oldImagePath);
                        }
                    }

                    var fileName = $"KH_{Guid.NewGuid()}.png";
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath)!);

                    await using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await updateDto.Image.CopyToAsync(stream);
                    }

                    user.Img = fileName;
                }

                var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == id);
                if (address != null)
                {
                    address.Ward = updateDto.Ward;
                    address.District = updateDto.District;
                    address.Province = updateDto.Province;
                    address.AddressLine = updateDto.AddressLine;
                    address.UpdatedAt = DateTime.Now;
                }
                else
                {
                    _context.Addresses.Add(new Address
                    {
                        UserId = user.UserId,
                        Ward = updateDto.Ward,
                        District = updateDto.District,
                        Province = updateDto.Province,
                        AddressLine = updateDto.AddressLine,
                        UpdatedAt = DateTime.Now
                    });
                }

                await _context.SaveChangesAsync();
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(ex.Message);
            }
        }

        [HttpDelete("Delete/{id}")]
        public async Task<ActionResult> Delete(int? id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var us = await _context.Users.FindAsync(id);
                if (us == null)
                {
                    return NotFound();
                }

                if (!string.IsNullOrEmpty(us.Img))
                {
                    var avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", us.Img);
                    if (System.IO.File.Exists(avatarPath))
                    {
                        System.IO.File.Delete(avatarPath);
                    }
                }

                var userRoles = _context.Set<Dictionary<string, object>>("UserRole")
                    .Where(x => x["UserId"].Equals(id));

                _context.Set<Dictionary<string, object>>("UserRole").RemoveRange(userRoles);

                var addresses = _context.Addresses.Where(x => x.UserId == id);
                if (addresses.Any())
                {
                    _context.Addresses.RemoveRange(addresses);
                }

                var wishlists = _context.Wishlists.Where(x => x.UserId == id);
                if (wishlists.Any())
                {
                    _context.Wishlists.RemoveRange(wishlists);
                }

                var cart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == id);
                if (cart != null)
                {
                    var cartItems = _context.CartItems.Where(x => x.CartId == cart.CartId);
                    if (cartItems.Any())
                    {
                        _context.CartItems.RemoveRange(cartItems);
                    }

                    _context.Carts.Remove(cart);
                }

                var notifications = _context.UserNotifications.Where(x => x.UserId == id);
                if (notifications.Any())
                {
                    _context.UserNotifications.RemoveRange(notifications);
                }

                _context.Users.Remove(us);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã xóa khách hàng." });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPost("BanUser")]
        public async Task<JsonResult> BanUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã chặn người dùng" });
        }

        [HttpPost("UnBanUser")]
        public async Task<JsonResult> UnBanUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }

            user.IsActive = true;
            user.UpdatedAt = DateTime.Now;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã bỏ chặn người dùng" });
        }

        private bool CheckPhoneNumberExist(string phoneNumber)
        {
            return !_context.Users.Any(x => x.PhoneNumber == phoneNumber);
        }

        private bool CheckEmailExist(string email)
        {
            return !_context.Users.Any(x => x.Email == email);
        }

        private bool CheckPhoneNumberExistForUpdate(string phoneNumber, int userId)
        {
            return !_context.Users.Any(x => x.PhoneNumber == phoneNumber && x.UserId != userId);
        }

        private bool CheckEmailExistForUpdate(string email, int userId)
        {
            return !_context.Users.Any(x => x.Email == email && x.UserId != userId);
        }
    }

    internal static class RouteValueDictionaryExtensions
    {
        public static RouteValueDictionary Append(this RouteValueDictionary values, KeyValuePair<string, object?> item)
        {
            values[item.Key] = item.Value;
            return values;
        }
    }
}
