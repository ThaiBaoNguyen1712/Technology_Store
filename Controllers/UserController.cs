﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Identity.Client;
using Newtonsoft.Json;
using NuGet.Protocol;
using System.Globalization;
using System.Security.Claims;
using Tech_Store.Events;
using Tech_Store.Helper;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.DTO.Authentication;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services.Admin.NotificationServices;

namespace Tech_Store.Controllers
{
    [Authorize]
    [Route("User")]
    public class UserController : BaseController
    {
        private readonly NotificationService _notificationService;
        public UserController(ApplicationDbContext context, NotificationService notificationService) : base(context) {
        _notificationService = notificationService;
        }
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
                ViewBag.Address_User = $"{address.AddressLine},{ward?.Name}, {district?.Name}, {province?.Name}";
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

                await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Thay đổi TT tài khoản", "Thay đổi thông tin tài khoản thành công", "success", "#", new List<int> { parsedUserId });

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
        public async Task<IActionResult> ChangePersonalInfo(string LastName, string FirstName, string Email, string PhoneNumber, IFormFile ImageAvatar)
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

                if (ImageAvatar != null)
                {
                    // Kiểm tra và xóa ảnh cũ
                    if (!string.IsNullOrEmpty(user.Img) && user.Img != "none.png") // Kiểm tra nếu có ảnh cũ
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
                        await ImageAvatar.CopyToAsync(stream);
                    }

                    user.Img = fileName; // Cập nhật đường dẫn ảnh mới
                }

                await _context.SaveChangesAsync();

                await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Thay đổi TT tài khoản", "Thay đổi thông tin tài khoản thành công", "success", "#", new List<int> { parsedUserId });

                return Json(new { success = true, message = "Thay đổi thông tin thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }
        #endregion

        [HttpPost("ToCheckout")]
        public IActionResult ToCheckout([FromBody] ListItemCartDTo listItemCartDTo)
        {
            if (!CheckUserInfoRequire())
            {
                return Json(new { success = false, message = "Cần phải cung cấp thông tin trước khi đến phần thanh toán." });
            }
            if (listItemCartDTo == null || listItemCartDTo.cartDTos == null || !listItemCartDTo.cartDTos.Any())
            {
                return Json(new { success = false, message = "Không có sản phẩm nào trong giỏ hàng." });
            }

            // Lưu vào Session dưới dạng chuỗi JSON
            HttpContext.Session.SetString("CartItems", JsonConvert.SerializeObject(listItemCartDTo));

            return Json(new { success = true, redirectUrl = Url.Action("Checkout", "Payment") });
        }

        #region Cart 
        [HttpPost("AddToCart")]
        public async Task<JsonResult> AddToCart(int itemId, int quantity)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { success = false, message = "Cần đăng nhập trước khi thêm sản phẩm vào giỏ hàng" });
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
            if (userId == null)
            {
                return Json(new { success = false, message = "Đã có lỗi xác thực" });
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


        #region ViewOrder
        [Route("MyOrders")]
        public IActionResult MyOrders()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            //Thêm dictionary để tạo o ánh xạ sắp xếp thứ tự 
            var statusOrder = new Dictionary<string, int>
                {
                    { "pending", 1 },
                    { "confirmed", 2 },
                    { "shipping", 3 },
                     { "delivered", 4 },
                    { "completed", 5},
                    { "refunded", 6},
                    { "cancelled", 7 }
                };
            var orders = _context.Orders
                 .Include(o => o.OrderItems)
                     .ThenInclude(oi => oi.VarientProduct)
                         .ThenInclude(vp => vp.Product)
                 .Include(o => o.Payments)
                 .Where(o => o.UserId == int.Parse(userId))
                 .AsEnumerable() // Chuyển sang xử lý trên client
                 .OrderBy(o => statusOrder[o.OrderStatus.ToLower()]) // Sắp xếp theo trạng thái
                 .ThenByDescending(o => o.OrderDate) // Sau đó sắp xếp theo ngày đặt hàng
                 .ToList();



            // Map dữ liệu sang ViewModel
            var orderViewModel = orders.Select(o => new OrderViewModelClient
            {
                Id = o.OrderId,
                ImageUrl = o.OrderItems.FirstOrDefault()?.VarientProduct?.Product?.Image ?? "/default-image.jpg", // Kiểm tra null và thêm ảnh mặc định
                variantIds = o.OrderItems.Select(x => x.VarientProductId).ToArray(),
                OrderStatus = o.OrderStatus,
                Is_Reviewed = o.IsReviewed ?? false, // Nếu null, đặt mặc định là false
                OrderDate = o.OrderDate ?? DateTime.MinValue, // Nếu null, đặt mặc định là ngày nhỏ nhất

                Amount = o.TotalAmount
            }).ToList();


            ViewBag.OrderCount = orderViewModel.Count();

            // Trả về View với ViewModel
            return View(orderViewModel);
        }


        [HttpPost("CancelOrder")]
        public async Task<JsonResult> CancelOrder(int orderId, string reason)
        {
            if (orderId == 0)
            {
                return Json(new { success = false, message = "Không gửi được mã đơn hàng" });
            }
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId.Equals(orderId));
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }
            order.Note = reason;
            order.OrderStatus = "Cancelled";
            await _context.SaveChangesAsync();

            //Gửi thông báo đến admin và client
            await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Hủy đơn hàng", $"Đơn hàng {order.OrderId} đã được hủy", "info", $"/user/MyOrders/OrderDetail/{order.OrderId}", new List<int> { order.UserId});

            await _notificationService.NotifyAsync(NotificationTarget.Admins, "Đơn hàng bị hủy", $"Đơn hàng {order.OrderId} đã bị hủy bởi khách hàng","danger", $"Admin/Orders/View/{order.OrderId}");

            return Json(new { success = true, message = "Đơn hàng đã hủy" });
        }

        [HttpPost("SendReview")]
        public async Task<JsonResult> SendReview(int orderId, int[] variantIds, string content, int starPoint)
        {
            // Kiểm tra điều kiện dữ liệu đầu vào
            if (orderId == 0 || string.IsNullOrEmpty(content))
            {
                return Json(new { success = false, message = "Có lỗi khi gửi thông tin" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Duyệt qua các variantIds và tạo các đánh giá
            foreach (var variant in variantIds)
            {
                var product = _context.VarientProducts
               .Where(x => x.VarientId == variant)
               .Select(x => new { x.ProductId, x.VarientId })
               .FirstOrDefault();

                var review = new Review
                {
                    UserId = int.Parse(userId),
                    ProductId = (int)product.ProductId,
                    VarientId = variant,
                    Rating = starPoint,
                    Comment = content,
                    ReviewDate = DateTime.Now,
                };

                // Thêm review vào context
                _context.Reviews.Add(review);
            }
            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId);
            order.IsReviewed = true;
            // Lưu thay đổi vào cơ sở dữ liệu
            await _context.SaveChangesAsync();

            //Gửi thông báo cho admin
            await _notificationService.NotifyAsync(NotificationTarget.Admins, "Đánh giá mới", $"Đánh giá của đơn hàng {order.OrderId}", "info" ,"/Admin/Orders/completed");

            return Json(new { success = true, message = "Gửi đánh giá thành công" });
        }

        [Route("MyOrders/OrderDetail/{orderId}")]
        public IActionResult OrderDetail(int orderId)
        {
            // Lấy thông tin đơn hàng từ cơ sở dữ liệu
            var order = _context.Orders
                .Include(x => x.Payments)
                .Include(x => x.User)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.VarientProduct)
                    .ThenInclude(x => x.Product)
                .FirstOrDefault(x => x.OrderId == orderId); // Lọc theo orderId

            if (order == null)
            {
                return NotFound("Order not found");
            }
            // Read the JSON file
            var jsonString = System.IO.File.ReadAllText("wwwroot/Province_VN.json");
            var provinces = JsonConvert.DeserializeObject<List<Province>>(jsonString);

            // Find the province, district, and ward by ID
            var province = provinces?.FirstOrDefault(p => p.Code == int.Parse(order.ShippingAddress.Province));
            var district = province?.Districts?.FirstOrDefault(d => d.Code == int.Parse(order.ShippingAddress.District));
            var ward = district?.Wards?.FirstOrDefault(w => w.Code == int.Parse(order.ShippingAddress.Ward));
            // Map dữ liệu sang ViewModel
            var result = new OrderDetailVM
            {
                OrderId = order.OrderId,
                TotalPrice = order.TotalAmount.ToString("C0", new CultureInfo("vi-VN")),
                DiscountPrice = (order.DeductAmount + order.DiscountAmount)?.ToString("C0", new CultureInfo("vi-VN")) ?? "0 ₫", // Format giá trị nếu cần
                ShippingPrice = order.ShippingAmount?.ToString("C0", new CultureInfo("vi-VN")),
                PaymentMethod = order.Payments.FirstOrDefault()?.PaymentMethod ?? "N/A",
                OriginTotalPrice = (decimal)order.OriginAmount, // Tính tổng giá gốc
                OrderStatus = order.OrderStatus,
                User = new Generate_User
                {
                    FullName = order.User.LastName + " " + order.User.FirstName,
                    PhoneNumber = order.User.PhoneNumber,
                    Address = order.ShippingAddress.AddressLine + ", " + province?.Name + ", " + district.Name + ", " + ward.Name,
                },
                ListVarientProduct = order.OrderItems.Select(item => new ProductVariant
                {
                    ProductId = item.ProductId,
                    VarientProductId = item.VarientProduct.VarientId,
                    Price = item.Price.ToString("C0", new CultureInfo("vi-VN")),
                    ImageUrl = item.Product.Image,
                    Quantity = item.Quantity,
                    NameProduct = item.Product.Name,
                    Attributes = item.VarientProduct.Attributes,
                    Slug = item.Product.Slug
                }).ToList()
            };

            // Truyền ViewModel sang view
            return View(result);
        }

        #endregion

        #region WishList
        [HttpPost("AddToWishlist")]
        public async Task<JsonResult> AddtoWishlist(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Json(new { success = false, message = "Cần đăng nhập trước" });
            }

            if (productId == 0)
            {
                return Json(new { success = false, message = "Không nhận được mã sản phẩm" });
            }

            // Kiểm tra xem sản phẩm đã có trong danh sách yêu thích của người dùng chưa
            var existingWishlistItem = _context.Wishlists
                .FirstOrDefault(x => x.UserId == int.Parse(userId) && x.ProductId == productId);

            if (existingWishlistItem != null)
            {
                // Nếu sản phẩm đã tồn tại trong danh sách yêu thích
                return Json(new { success = false, message = "Sản phẩm đã có trong danh sách yêu thích" });
            }

            // Nếu sản phẩm chưa có trong danh sách yêu thích, thêm mới
            var wishlist = new Wishlist
            {
                UserId = int.Parse(userId),
                ProductId = productId,
                AddedDate = DateTime.Now
            };

            _context.Wishlists.Add(wishlist);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã thêm vào DS yêu thích" });
        }

        [HttpPost("RemoveItemFromWishlist")]
        public async Task<JsonResult> RemoveItemFromWishlist(int productId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var wishlist = await _context.Wishlists.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId) && x.ProductId == productId);
            if (wishlist == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm trong ds yêu thích" });
            }
            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã xóa sản phẩm ra khỏi ds" });
        }

        [Route("Wishlist")]
        public IActionResult Wishlist()
        {
            // Lấy UserId từ Claims
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra tính hợp lệ của userId
            if (string.IsNullOrEmpty(userId) || !int.TryParse(userId, out int parsedUserId))
            {
                // Trả về thông báo lỗi hoặc điều hướng đến trang đăng nhập
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách yêu thích của người dùng
            var wishlistItems = _context.Wishlists
                .Where(x => x.UserId == parsedUserId)
                .Distinct()
                .Select(x => new WishlistVM
                {
                    ProductId = x.Product.ProductId,
                    ProductName = x.Product.Name,
                    ImageUrl = x.Product.Image,
                    Slug = x.Product.Slug,
                    Price = x.Product.SellPrice != null
                        ? x.Product.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN"))
                        : "Liên hệ", // Giá trị mặc định nếu giá bán null
                    CreatedAt = x.AddedDate ?? DateTime.MinValue // Giá trị mặc định nếu ngày null
                })
                .ToList();

            // Trả về view với dữ liệu danh sách yêu thích
            return View(wishlistItems);
        }

        #endregion

        #region Comment
        [HttpPost("CreateComment")]
        public async Task<JsonResult> CreateComment([FromBody] CommentDTo commentDto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }

            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var comment = new Comment
                {
                    UserId = int.Parse(userId),
                    Content = commentDto.Content,
                    ProductId = commentDto.ProductId,
                    ParentCommentId = commentDto.ParentCommentId == 0 ? null : commentDto.ParentCommentId,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    Status = true
                };

                _context.Comments.Add(comment);
                await _context.SaveChangesAsync();

                // Tạo thông báo cho người dùng (ví dụ admin và người dùng liên quan)
                var product_get = await _context.Products.Select(x => new {x.ProductId,x.Slug,x.Name}).FirstOrDefaultAsync(x=>x.ProductId == commentDto.ProductId);

                if (commentDto.ParentCommentId != 0 || commentDto.ParentCommentId.HasValue)
                {
                    var cmt_parent = await _context.Comments.FirstOrDefaultAsync(s=>s.CommentId == commentDto.ParentCommentId);
                    var user_name = await _context.Users.Where(x => x.UserId == int.Parse(userId)).Select(u => new { u.LastName, u.FirstName }).FirstOrDefaultAsync();
                    //Nếu comment là parent và không phải là tự trả lời
                    if (cmt_parent != null && cmt_parent.UserId != int.Parse(userId))
                    {
                        await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Trả lời bình luận của bạn", $"Bình luận sản phẩm {product_get.Name} " +
                            $"đã được trả lời bởi {user_name.LastName + " " + user_name.FirstName}.", "new comment", $"/View/{product_get.Slug}", new List<int> { cmt_parent.UserId });
                    }
                }
                //Nếu như cmt của admin thì ko cần thông báo tới admin nữa
                if(int.Parse(userId) != 1)
                {
                    var userIds = new List<string>
                    {
                        "1", // Admin có UserId = "1" (kiểu string)
                        userId // ID của người dùng đang đăng nhập (lấy từ ClaimTypes.NameIdentifier)
                    };
                    await _notificationService.NotifyAsync(NotificationTarget.Admins, "Bình luận mới", $"Bình luận mới được thêm vào sản phẩm {product_get.Name}.", "new comment", $"/View/{product_get.Slug}");
                }
               

                return Json(new { success = true, comment });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        [HttpPost("UpdateComment")]
        public async Task<JsonResult> UpdateComment([FromBody] CommentDTo commentDto)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Không nhận được dữ liệu đầu vào" });
            }
            try
            {
                var comment = await _context.Comments.FirstOrDefaultAsync(x => x.CommentId.Equals(commentDto.Id));
                if (comment == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy bình luận" });
                }
                comment.Content = commentDto.Content;
                comment.UpdatedAt = DateTime.Now;
                await _context.SaveChangesAsync();
                return Json(new { success = true, comment });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.ToString() });
            }
        }

        [HttpPost("DeleteComment")]
        public async Task<JsonResult> DeleteComment(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Lấy bình luận theo ID và kiểm tra quyền sở hữu
            var comment = await _context.Comments.FirstOrDefaultAsync(x => x.CommentId == id && x.UserId == int.Parse(userId));
            if (comment == null)
            {
                return Json(new { success = false, message = "Không tìm thấy bình luận" });
            }

            // Kiểm tra nếu là bình luận cha
            if (comment.ParentCommentId == null || comment.ParentCommentId == 0)
            {
                // Lấy tất cả bình luận con liên quan
                var childComments = await _context.Comments.Where(x => x.ParentCommentId == id).ToListAsync();

                // Xóa bình luận con
                if (childComments.Any())
                {
                    _context.Comments.RemoveRange(childComments);
                }
            }

            // Xóa bình luận chính
            _context.Comments.Remove(comment);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Đã xóa bình luận thành công" });
        }

        #endregion
    }
}
