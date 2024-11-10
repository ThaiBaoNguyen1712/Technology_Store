using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using Tech_Store.Helper;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.DTO.Authentication;

namespace Tech_Store.Controllers
{
    [Route("User")]
    public class UserController : BaseController
    {
        public UserController(ApplicationDbContext context): base(context) { }
        public IActionResult Index()
        {
            return View();
        }
        [Route("Cart")]
        public async Task<IActionResult> Cart()
        {
            // Lấy ID người dùng hiện tại
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out var userId))
            {
                return BadRequest("Invalid user ID.");
            }

            // Retrieve the cart with related data
            var cart = await _context.Carts
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.VarientProduct)
                        .ThenInclude(vp => vp.Product)
                .SingleOrDefaultAsync(x => x.UserId == userId);

            if (cart == null)
            {
                return NotFound();
            }
            // lấy các sản phẩm liên quan theo danh mục, duy nhất distinct
            var cartProductCategories = cart.CartItems
                .Select(ci => ci.VarientProduct.Product.CategoryId)
                .Distinct()
                .ToList();

            var cartProductIds = cart.CartItems
                .Select(ci => ci.VarientProduct.Product.ProductId)
                .Distinct()
                .ToList();

            //Tìm các sản phẩm liên quan theo danh mục
            var relatedByCategory = await _context.Products
                .Where(p => cartProductCategories.Contains(p.CategoryId))
                .Where(p => !cartProductIds.Contains(p.ProductId))
                .ToListAsync();

            // lấy duy nhất các sp liên quan theo tên
            var cartProductNames = cart.CartItems
                .Select(ci => ci.VarientProduct.Product.Name)
                .Distinct()
                .ToList();

            // tìm các sản phẩm liên quan theo tên giống các SP có sẵn trong giỏ hàng
            var relatedByName = await _context.Products
                .Where(p => !cartProductIds.Contains(p.ProductId))
                .ToListAsync(); // Using ToListAsync here to perform client-side filtering

           
            relatedByName = relatedByName
                .Where(p => cartProductNames.Any(name =>
                    p.Name.Contains(name, StringComparison.OrdinalIgnoreCase)))
                .ToList();

            //tìm sản phẩm liên quan theo thương hiệu
            var cartProductBrands = cart.CartItems
                .Select(ci => ci.VarientProduct.Product.BrandId)
                .Distinct()
                .ToList();

            var relatedByBrand = await _context.Products
                .Where(p => cartProductBrands.Contains(p.BrandId))
                .Where(p => !cartProductIds.Contains(p.ProductId))
                .ToListAsync();

            //Kết hợp các bản ghi và loại bỏ các trùng lặp
            var productRelated = relatedByCategory
                .Union(relatedByName)
                .Union(relatedByBrand)
                .Take(10)
                .Distinct() // Ensure we have unique products
                .ToList();

            // Pass related products to the view
            ViewBag.Product_Related = productRelated;


            return View(cart);
        }


        #region Information
        [Route("Information")]
        public async Task<IActionResult> Information()
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
        public async Task<JsonResult> ChangeAddress(string addressLine,string ward,string province,string district)
        {
            try
            {
                if(!ModelState.IsValid)
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
                        AddressLine =addressLine,
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
        public async Task<IActionResult> ChangePersonalInfo(string LastName,string FirstName,string Email,string PhoneNumber)
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
            catch(Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        #endregion

        [HttpPost("ToCheckout")]
        public IActionResult ToCheckout([FromBody] ListItemCartDTo listItemCartDTo)
        {
            if (listItemCartDTo == null || listItemCartDTo.cartDTos == null || !listItemCartDTo.cartDTos.Any())
            {
                return NotFound("Không có sản phẩm nào trong giỏ hàng.");
            }

            // Lưu vào Session dưới dạng chuỗi JSON
            HttpContext.Session.SetString("CartItems", JsonConvert.SerializeObject(listItemCartDTo));
            return Ok(new { redirectUrl = Url.Action("Checkout","Payment") });
        }
        #region Cart 
        [HttpPost("AddToCart")]
        public async Task<JsonResult> AddToCart(int itemId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Cần đăng nhập" });
            }

            // Kiểm tra tính hợp lệ của quantity
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra sản phẩm có tồn tại hay không
            var product = await _context.VarientProducts.FindAsync(itemId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tồn tại" });
            }

            var cart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
            // Nếu chưa tồn tại giỏ hàng, thì tạo
            if (cart == null)
            {
                cart = new Cart
                {
                    UserId = int.Parse(userId),
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            // Tìm sản phẩm trong giỏ hàng
            var existingCartItem = await _context.CartItems.FirstOrDefaultAsync(x => x.ProductId == itemId && x.CartId == cart.CartId);
            if (existingCartItem != null)
            {
                existingCartItem.Quantity += quantity;
            }
            else
            {
                var productId = await _context.VarientProducts
                 .Where(x => x.VarientId == itemId) // Sử dụng Where để lọc
                 .Select(x => new { x.ProductId, x.VarientId }) // Dùng anonymous type để lấy ProductId và VarientId
                 .FirstOrDefaultAsync();

                var cartItem = new CartItem
                {
                    CartId = cart.CartId,
                    ProductId = (int)productId.ProductId,
                    VarientProductId = itemId,
                    Quantity = quantity
                };
                _context.CartItems.Add(cartItem);
            }

            // Cập nhật thời gian cập nhật giỏ hàng
            cart.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã thêm vào giỏ hàng" });
        }


        [HttpPost("Update-Quantity")]
        public async Task<JsonResult> UpdateQuantity(int itemId, int quantity)
        {
            if (itemId == 0 || quantity <= 0)
            {
                return Json(new { success = false, message = "Đầu vào không hợp lệ" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
            if (cart == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giỏ hàng của người dùng" });
            }

            var item = await _context.CartItems.FirstOrDefaultAsync(x => x.ProductId == itemId && x.CartId == cart.CartId);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }

            item.Quantity = quantity;
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Cập nhật thành công" });
        }

        [HttpPost("Delete-CartItem")]
        public async Task<JsonResult> DeleteCartItem(int itemId)
        {
            if (itemId == 0)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm cần xóa" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var cart = await _context.Carts.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
            if (cart == null)
            {
                return Json(new { success = false, message = "Không tìm thấy giỏ hàng của người dùng" });
            }

            var item = await _context.CartItems.FirstOrDefaultAsync(x => x.ProductId == itemId && x.CartId == cart.CartId);
            if (item == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
            }

            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Xóa thành công" });
        }
        #endregion

        #region ChangeInformation
        [HttpPost("ChangeInformation")]
        public async Task<JsonResult> ChangeInformation(UserDTo user)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if(userId == null)
            {
                return Json(new { success = false , message ="Đã có lỗi xác thực"});
            }
            var user_get = await _context.Users.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
            user_get.LastName = user.LastName;
            user_get.FirstName = user.FirstName;
            user_get.Email = user.Email;
            user_get.PhoneNumber = user.PhoneNumber;

            var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
            address.AddressLine = user.AddressLine;
            address.Ward = user.Ward;
            address.District = user.District;
            address.Province = user.Province;

            _context.SaveChangesAsync();
            return Json(new { success = true, message = "Thay đổi thành công" });

        }

        #endregion
    }
}
