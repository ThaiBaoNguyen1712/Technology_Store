using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualBasic;
using OfficeOpenXml.FormulaParsing.Excel.Functions.Math;
using System.ComponentModel;
using System.Diagnostics;
using System.Net;
using System.Transactions;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.ViewModel;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class UsersController : BaseAdminController
    {
        public UsersController(ApplicationDbContext context) : base(context) { }

        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var list_Users = await _context.Users
                .OrderByDescending(x => x.UserId)
                .Select(x => new UserVM
                {
                    UserId = x.UserId,
                    LastName = x.LastName,
                    FirstName = x.FirstName,
                    PhoneNumber = x.PhoneNumber,
                    Email = x.Email,
                    ImageUrl = x.Img,
                    IsActive = (bool)x.IsActive,
                    OrderCount = x.Orders.Count
                })
                .ToListAsync();

            return View(list_Users);
        }

        [HttpPost]
        [Route("Filter")]
        public IActionResult Filter(string? nameCustomer, string? status, string? email, decimal? revenueFrom, decimal? revenueTo,
                 DateTime? CreatedDateFrom, DateTime? CreatedDateTo, string? phoneNumber)
        {
            var users = _context.Users
                .Include(p => p.Orders).ThenInclude(t => t.Payments)
                .AsQueryable();

            // Áp dụng các tiêu chí lọc
            if (!string.IsNullOrEmpty(nameCustomer))
                users = users.Where(p => p.FirstName.Contains(nameCustomer) || p.LastName.Contains(nameCustomer));

            if (!string.IsNullOrEmpty(status) && bool.TryParse(status,out bool isActive))
                users = users.Where(p => p.IsActive == isActive);

            if (revenueFrom.HasValue || revenueTo.HasValue)
            {
                users = users.Where(p =>
                    (!revenueFrom.HasValue || p.Orders.Sum(s => s.TotalAmount) >= revenueFrom.Value) &&
                    (!revenueTo.HasValue || p.Orders.Sum(s => s.TotalAmount) <= revenueTo.Value)
                );
            }

            if (!string.IsNullOrEmpty(email))
                users = users.Where(p => p.Email.Contains(email));


            if (CreatedDateFrom.HasValue || CreatedDateTo.HasValue)
                users = users.Where(p =>
                    (!CreatedDateFrom.HasValue || p.CreatedAt >= CreatedDateFrom.Value) &&
                    (!CreatedDateTo.HasValue || p.CreatedAt <= CreatedDateTo.Value)
                );

            if (!string.IsNullOrEmpty(phoneNumber))
                users = users.Where(p => p.PhoneNumber.Contains(phoneNumber));

            // Chuyển đổi sang view model để trả về JSON
            var result = users.OrderByDescending(p => p.UserId)
                .Take(100)
                .Select(p => new UserVM
                {
                   UserId = p.UserId,
                   FirstName = p.FirstName,
                   LastName = p.LastName,
                   ImageUrl = p.Img,
                   Email = p.Email,
                   IsActive = (bool)p.IsActive,
                   OrderCount = p.Orders.Count(),
                   PhoneNumber = p.PhoneNumber
                })
                .ToList();

            return Json(result);
        }


        [AutoValidateAntiforgeryToken]
        [HttpPost("Create")]
        [DisableRequestSizeLimit]
        [RequestFormLimits(MultipartBodyLengthLimit = int.MaxValue, ValueLengthLimit = int.MaxValue)]
        public async Task<IActionResult> Create([FromForm] UserDTo us)
        {
            if (!CheckEmailExist(us.Email))
            {
                return BadRequest("Email đã tồn tại");
            }
            if (!CheckPhoneNumberExist(us.PhoneNumber))
            {
                return BadRequest("SDT đã tồn tại");
            }
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                if (ModelState.IsValid)
                {
                    var user = new User
                    {
                        FirstName = us.FirstName,
                        LastName = us.LastName,
                        Email = us.Email,
                        PhoneNumber = us.PhoneNumber,
                        IsActive = us.IsActive,
                    };

                    // Thêm người dùng
                    _context.Users.Add(user);
                    // Lưu User để lấy ID trước
                    await _context.SaveChangesAsync();

                    // Lưu hình ảnh vào file
                    if (us.Image != null && us.Image.Length > 0)
                    {
                        var fileName = $"KH_{Guid.NewGuid()}.png";
                        var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", fileName);
                        Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                        using (var stream = new FileStream(imagePath, FileMode.Create))
                        {
                            await us.Image.CopyToAsync(stream);
                        }

                        user.Img = fileName;
                    }
                    else
                    {
                        user.Img = "none.jpg";
                    }


                    // Sử dụng bảng UserRole riêng biệt:
                    var userRole = new Dictionary<string, object>
                    {
                        { "UserId", user.UserId },
                        { "RoleId", 2 }
                    };

                    _context.Set<Dictionary<string, object>>("UserRole").Add(userRole);

                    // Thêm địa chỉ
                    var address = new Address
                    {
                        UserId = user.UserId,
                        Ward = us.Ward,
                        District = us.District,
                        Province = us.Province,
                        AddressLine = us.AddressLine,
                    };
                    _context.Addresses.Add(address);
                    
                    // Thêm giỏ hàng
                    var cart = new Cart
                    {
                        UserId = user.UserId,
                    };
                    _context.Carts.Add(cart);

                    // Lưu toàn bộ thay đổi
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    return Ok();
                }
                else
                {
                    return BadRequest("Invalid model state");
                }
            }
            catch (Exception ex)
            {
                // Rollback transaction nếu có lỗi xảy ra
                await transaction.RollbackAsync();
                Console.WriteLine(ex.ToString());
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var us = await _context.Users.FindAsync(id);
            var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == id);
            if (us == null)
                return NotFound();

            var usDto = new UserDTo
            {
                UserId = us.UserId,
                FirstName = us.FirstName,
                LastName = us.LastName,
                Email = us.Email,
                PhoneNumber = us.PhoneNumber,
                IsActive = (bool)us.IsActive,
                ImageUrl = us.Img,
                //Address
                AddressLine = address?.AddressLine ?? "", 
                Ward = address?.Ward ?? "",               
                District = address?.District ?? "",     
                Province = address?.Province ?? "",
                
            };
            return Json(usDto);
        }

        [HttpPut("Update/{id}")]
        public async Task<IActionResult> Update(int id, [FromForm] UserDTo updateDto)
        {
            // Kiểm tra tính hợp lệ của ModelState
            if (!ModelState.IsValid)
            {
                return BadRequest("Thiếu thông tin");
            }

            // Kiểm tra số điện thoại và email có tồn tại hay không
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
                // Tìm người dùng từ database
                var user = await _context.Users.FindAsync(id);
                if (user == null)
                {
                    return NotFound();
                }

                // Cập nhật thông tin người dùng
                user.FirstName = updateDto.FirstName;
                user.LastName = updateDto.LastName;
                user.PhoneNumber = updateDto.PhoneNumber.Trim();
                user.IsActive = updateDto.IsActive;

                // Xóa ảnh cũ nếu có và cập nhật ảnh mới
                if (updateDto.Image != null && updateDto.Image.Length > 0)
                {
                    // Kiểm tra và xóa ảnh cũ
                    if (!string.IsNullOrEmpty(user.Img)) // Kiểm tra nếu có ảnh cũ
                    {
                        var oldImagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", user.Img);
                        if (System.IO.File.Exists(oldImagePath)) // Nếu tệp tồn tại
                        {
                            System.IO.File.Delete(oldImagePath); // Xóa ảnh cũ
                        }
                    }

                    // Lưu hình ảnh mới
                    var fileName = $"KH_{Guid.NewGuid()}.png";
                    var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", fileName);
                    Directory.CreateDirectory(Path.GetDirectoryName(imagePath));

                    using (var stream = new FileStream(imagePath, FileMode.Create))
                    {
                        await updateDto.Image.CopyToAsync(stream);
                    }

                    user.Img = fileName; // Cập nhật đường dẫn ảnh mới
                }

                // Kiểm tra và cập nhật địa chỉ
                var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == id);
                if (address != null)
                {
                    // Cập nhật các thuộc tính của địa chỉ
                    address.Ward = updateDto.Ward;
                    address.District = updateDto.District;
                    address.Province = updateDto.Province;
                    address.AddressLine = updateDto.AddressLine;
                }
                else
                {
                    // Nếu không tìm thấy địa chỉ, có thể thêm một địa chỉ mới (tuỳ thuộc vào yêu cầu của bạn)
                    var newAddress = new Address
                    {
                        UserId = user.UserId,
                        Ward = updateDto.Ward,
                        District = updateDto.District,
                        Province = updateDto.Province,
                        AddressLine = updateDto.AddressLine,
                    };
                    _context.Addresses.Add(newAddress);
                }

                // Lưu thay đổi vào database
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
                // Tìm người dùng
                var us = await _context.Users.FindAsync(id);
                if (us == null)
                {
                    return NotFound();
                }

                // Xóa ảnh đại diện nếu tồn tại
                if (!string.IsNullOrEmpty(us.Img)) // Kiểm tra xem có tồn tại đường dẫn ảnh không
                {
                    var avatarPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Upload", "Avatar", us.Img);
                    if (System.IO.File.Exists(avatarPath)) // Kiểm tra xem file có tồn tại không
                    {
                        System.IO.File.Delete(avatarPath); // Xóa file
                    }
                }

                // Xóa bản ghi trong bảng UserRole
                var userRole = await _context.Set<Dictionary<string, object>>("UserRole")
                    .FirstOrDefaultAsync(x => x["UserId"].Equals(id));
                if (userRole != null)
                {
                    _context.Set<Dictionary<string, object>>("UserRole").Remove(userRole);
                }


                //Xóa address
                var address = await _context.Addresses.FirstOrDefaultAsync(x=>x.UserId == id);
                if (address != null)
                {
                    _context.Addresses.Remove(address);
                }

                // Xoá wishlist của người dùng
                var wishlist = await _context.Wishlists.FirstOrDefaultAsync(x => x.UserId == id);
                if (wishlist != null)
                {
                    _context.Wishlists.Remove(wishlist);
                }

                // Tìm giỏ hàng của người dùng
                var cart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == id);
                if (cart != null)
                {
                    // Xoá tất cả các mục trong giỏ hàng
                    var cartItems = _context.CartItems.Where(x => x.CartId == cart.CartId);
                    if(cartItems.Any())
                    {
                        _context.CartItems.RemoveRange(cartItems);
                    }

                    // Xoá giỏ hàng
                    _context.Carts.Remove(cart);
                }

                // Xoá người dùng
                _context.Users.Remove(us);

                // Lưu thay đổi
                await _context.SaveChangesAsync();

                // Commit transaction
                await transaction.CommitAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                // Rollback nếu có lỗi xảy ra
                Debugger.Break(); // Dừng tại đây
                await transaction.RollbackAsync();
                return BadRequest(new { message = ex.Message });
            }
        }
        [HttpPost("BanUser")]
        public async Task<JsonResult> BanUser(int id)
        {
            var user = await _context.Users.FirstOrDefaultAsync(x=>x.UserId == id);
            if (user == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng" });
            }
            user.IsActive = false;
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
}
