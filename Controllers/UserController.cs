using Microsoft.AspNetCore.Authorization;
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
using Tech_Store.Models.Enums;
using Tech_Store.Models.ViewModel;
using Tech_Store.Services;
using Tech_Store.Services.Admin.NotificationServices;
using Tech_Store.Services.Recommendation;

namespace Tech_Store.Controllers
{
    [Authorize]
    [Route("User")]
    public class UserController : BaseController
    {
        private readonly NotificationService _notificationService;
        private readonly IUserProductEventTrackingService _userProductEventTrackingService;

        public UserController(ApplicationDbContext context, NotificationService notificationService, IUserProductEventTrackingService userProductEventTrackingService) : base(context) {
            _notificationService = notificationService;
            _userProductEventTrackingService = userProductEventTrackingService;
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
            // Còn hàng và vẫn còn được đăng bán
            var relatedByCategory = await _context.Products
                .Where(p => cartProductCategories.Contains(p.CategoryId))
                .Where(p => !cartProductIds.Contains(p.ProductId))
                .Where(p=> p.Stock >=1 && p.Status !="outstock")
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
                return Json(new { success = false, message = "login require" });
            }

            // Kiểm tra tính hợp lệ của quantity
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // Kiểm tra sản phẩm có tồn tại hay không
            var product = await _context.VarientProducts.Include(x => x.Product).FirstOrDefaultAsync(x=>x.VarientId == itemId);
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

            try
            {
                await _userProductEventTrackingService.TrackAsync(new UserProductEventWriteRequest
                {
                    UserId = int.Parse(userId),
                    ProductId = product.ProductId ?? product.Product.ProductId,
                    ProductSysId = product.Product.ProductSysId,
                    EventType = UserProductEventType.AddCart,
                    Source = "user_add_to_cart"
                });
            }
            catch(Exception ex)
            {
                Console.WriteLine($"Lỗi khi theo dõi sản phẩm đã xem: {ex.Message}");
            }

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

            var trackedProductId = item.ProductId;
            _context.CartItems.Remove(item);
            await _context.SaveChangesAsync();

            var removedProduct = await _context.Products
                .AsNoTracking()
                .Where(x => x.ProductId == trackedProductId)
                .Select(x => new { x.ProductId, x.ProductSysId })
                .FirstOrDefaultAsync();

            if (removedProduct != null)
            {
                await _userProductEventTrackingService.TrackAsync(new UserProductEventWriteRequest
                {
                    UserId = int.Parse(userId),
                    ProductId = removedProduct.ProductId,
                    ProductSysId = removedProduct.ProductSysId,
                    EventType = UserProductEventType.RemoveCart,
                    Source = "user_delete_cart_item"
                });
            }

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
                ReviewItems = o.OrderItems.Select(x => new OrderReviewItemViewModel
                {
                    ProductId = x.ProductId,
                    VariantId = x.VarientProductId,
                    ProductName = x.VarientProduct?.Product?.Name ?? string.Empty,
                    Attributes = x.VarientProduct?.Attributes ?? string.Empty,
                    ImageUrl = x.VarientProduct?.Product?.Image ?? string.Empty
                }).ToList(),
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId) || !int.TryParse(userId, out var parsedUserId))
            {
                return Json(new { success = false, message = "Không xác định được người dùng" });
            }

            var order = await _context.Orders.FirstOrDefaultAsync(x => x.OrderId == orderId && x.UserId == parsedUserId);
            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            if (!string.Equals(order.OrderStatus, OrderStatusType.Pending.ToRecordValue(), StringComparison.OrdinalIgnoreCase))
            {
                return Json(new { success = false, message = "Đơn hàng này không thể hủy." });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                order.Note = reason?.Trim();
                order.OrderStatus = OrderStatusType.Cancelled.ToRecordValue();
                order.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = "Có lỗi xảy ra trong quá trình hủy đơn hàng" });
            }

            //Gửi thông báo đến admin và client
            await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Hủy đơn hàng", $"Đơn hàng {order.OrderId} đã được hủy", "info", $"/user/MyOrders/OrderDetail/{order.OrderId}", new List<int> { order.UserId});

            await _notificationService.NotifyAsync(NotificationTarget.Admins, "Đơn hàng bị hủy", $"Đơn hàng {order.OrderId} đã bị hủy bởi khách hàng","danger", $"Admin/Orders/View/{order.OrderId}");

            return Json(new { success = true, message = "Đơn hàng đã hủy" });
        }

        [HttpPost("SendReview")]
        public async Task<JsonResult> SendReview([FromBody] OrderReviewSubmitDto request)
        {
            if (request == null || request.OrderId <= 0 || request.Items == null || !request.Items.Any())
            {
                return Json(new { success = false, message = "Có lỗi khi gửi thông tin" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Json(new { success = false, message = "Không xác định được người dùng" });
            }

            var reviewItems = request.Items
                .Where(x => x.VariantId > 0)
                .Select(x => new OrderReviewItemSubmitDto
                {
                    VariantId = x.VariantId,
                    StarPoint = x.StarPoint,
                    Content = x.Content?.Trim() ?? string.Empty
                })
                .ToList();

            if (!reviewItems.Any())
            {
                return Json(new { success = false, message = "Không có sản phẩm nào để đánh giá" });
            }

            if (reviewItems.Any(x => x.StarPoint < 1 || x.StarPoint > 5 || string.IsNullOrWhiteSpace(x.Content)))
            {
                return Json(new { success = false, message = "Vui lòng nhập đủ số sao và nhận xét cho từng sản phẩm" });
            }

            var order = await _context.Orders
                .Include(x => x.OrderItems)
                .FirstOrDefaultAsync(x => x.OrderId == request.OrderId && x.UserId == int.Parse(userId));

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var validVariantIds = order.OrderItems
                .Select(x => x.VarientProductId)
                .Distinct()
                .ToHashSet();

            if (reviewItems.Any(x => !validVariantIds.Contains(x.VariantId)))
            {
                return Json(new { success = false, message = "Có sản phẩm không thuộc đơn hàng này" });
            }

            var variants = await _context.VarientProducts
                .Where(x => reviewItems.Select(item => item.VariantId).Contains(x.VarientId))
                .Select(x => new { x.VarientId, x.ProductId })
                .ToListAsync();

            foreach (var item in reviewItems)
            {
                var product = variants.FirstOrDefault(x => x.VarientId == item.VariantId);
                if (product == null)
                {
                    continue;
                }

                var existingReview = await _context.Reviews
                    .FirstOrDefaultAsync(x => x.UserId == int.Parse(userId) && x.VarientId == item.VariantId);

                if (existingReview != null)
                {
                    existingReview.ProductId = product.ProductId ?? 0;
                    existingReview.Rating = item.StarPoint;
                    existingReview.Comment = item.Content;
                    existingReview.ReviewDate = DateTime.Now;
                    existingReview.UpdatedAt = DateTime.Now;
                    continue;
                }

                _context.Reviews.Add(new Review
                {
                    UserId = int.Parse(userId),
                    ProductId = product.ProductId ?? 0,
                    VarientId = item.VariantId,
                    Rating = item.StarPoint,
                    Comment = item.Content,
                    ReviewDate = DateTime.Now,
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now
                });
            }

            order.IsReviewed = true;
            await _context.SaveChangesAsync();

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
                .Include(x => x.ShippingAddress)
                .Include(x => x.OrderItems)
                    .ThenInclude(x => x.VarientProduct)
                    .ThenInclude(x => x.Product)
                .Include(x => x.OrderItems)
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
            var shippingAddress = order.ShippingAddress;
            var provinceCode = TryParseAddressCode(shippingAddress?.Province);
            var districtCode = TryParseAddressCode(shippingAddress?.District);
            var wardCode = TryParseAddressCode(shippingAddress?.Ward);

            var province = provinceCode.HasValue
                ? provinces?.FirstOrDefault(p => p.Code == provinceCode.Value)
                : null;
            var district = districtCode.HasValue
                ? province?.Districts?.FirstOrDefault(d => d.Code == districtCode.Value)
                : null;
            var ward = wardCode.HasValue
                ? district?.Wards?.FirstOrDefault(w => w.Code == wardCode.Value)
                : null;

            var displayAddressParts = new[]
            {
                shippingAddress?.AddressLine,
                ward?.Name,
                district?.Name,
                province?.Name
            }.Where(part => !string.IsNullOrWhiteSpace(part));

            var displayAddress = displayAddressParts.Any()
                ? string.Join(", ", displayAddressParts)
                : "Chưa có địa chỉ giao hàng";
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
                    FullName = string.Join(" ", new[] { order.User?.LastName, order.User?.FirstName }.Where(part => !string.IsNullOrWhiteSpace(part))),
                    PhoneNumber = order.User?.PhoneNumber,
                    Address = displayAddress,
                },
                ListVarientProduct = order.OrderItems.Select(item => new ProductVariant
                {
                    ProductId = item.ProductId,
                    VarientProductId = item.VarientProduct.VarientId,
                    Price = item.Price.ToString("C0", new CultureInfo("vi-VN")),
                    ImageUrl = item.Product?.Image ?? item.VarientProduct?.Product?.Image,
                    Quantity = item.Quantity,
                    NameProduct = item.Product?.Name ?? item.VarientProduct?.Product?.Name,
                    Attributes = item.VarientProduct.Attributes,
                    Slug = item.Product?.Slug ?? item.VarientProduct?.Product?.Slug
                }).ToList()
            };

            // Truyền ViewModel sang view
            return View(result);
        }

        private static int? TryParseAddressCode(string? code)
        {
            return int.TryParse(code, out var parsedCode) ? parsedCode : null;
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

            var product_get = await _context.Products.Select(x => new { x.ProductId, x.ProductSysId, x.Name }).FirstOrDefaultAsync(x => x.ProductId == productId);

            await _userProductEventTrackingService.TrackAsync(new UserProductEventWriteRequest
            {
                UserId = int.Parse(userId),
                ProductId = product_get?.ProductId ?? productId,
                ProductSysId = product_get?.ProductSysId,
                EventType = UserProductEventType.WishlistAdd,
                Source = "user_add_to_wishlist"
            });

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
            var trackedProductId = wishlist.ProductId;
            _context.Wishlists.Remove(wishlist);
            await _context.SaveChangesAsync();

            var removedProduct = await _context.Products
                .AsNoTracking()
                .Where(x => x.ProductId == trackedProductId)
                .Select(x => new { x.ProductId, x.ProductSysId })
                .FirstOrDefaultAsync();

            if (removedProduct != null)
            {
                await _userProductEventTrackingService.TrackAsync(new UserProductEventWriteRequest
                {
                    UserId = int.Parse(userId),
                    ProductId = removedProduct.ProductId,
                    ProductSysId = removedProduct.ProductSysId,
                    EventType = UserProductEventType.WishlistRemove,
                    Source = "user_remove_wishlist"
                });
            }

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

        #region Generate Guest ID 
        [HttpGet("Generate_Guest_ID")]
        public async Task<int> generate_guest_Id()
        {
            return await Task.FromResult(Random.Shared.Next(1000, int.MaxValue));
        }

        #endregion
    }
}
