using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using Tech_Store.Events;
using Tech_Store.Extensions;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using Tech_Store.Models.DTO.Payment;
using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.DTO.Payment.Client.Momo;
using Tech_Store.Models.DTO.Payment.Client.VnPay;
using Tech_Store.Services.MomoServices;
using Tech_Store.Services.NotificationServices;
using Tech_Store.Services.VNPayServices;

namespace Tech_Store.Controllers
{
    [Authorize]
    [Route("Payment")]
    public class PaymentController : BaseController
    {
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly NotificationService _notificationService;
        public PaymentController(ApplicationDbContext _context, IVnPayService vnPayService,IMomoService momoService, NotificationService notificationService) : base(_context)
        {
            _vnPayService = vnPayService;
            _momoService = momoService;
            _notificationService = notificationService;
        }


        [Route("Checkout")]
        public async Task<IActionResult> Checkout()
        {
            
            // Sử dụng GetString để lấy giá trị từ Session
            var cartItemsJson = HttpContext.Session.GetString("CartItems");

            if (cartItemsJson != null)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var user = await _context.Users.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
                var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
                if (!string.IsNullOrEmpty(address.AddressLine) &&
                    !string.IsNullOrEmpty(address.Province) &&
                    !string.IsNullOrEmpty(address.Ward) &&
                    !string.IsNullOrEmpty(address.District))
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
                var listItemCartDTo = JsonConvert.DeserializeObject<ListItemCartDTo>(cartItemsJson);

                var user_return = new UserDTo
                {
                    FirstName = user.FirstName,
                    LastName = user.LastName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    AddressLine = address.AddressLine,
                    Ward = address.Ward,
                    Province = address.Province,
                    District = address.District
                };
                // Giả sử bạn có một service để lấy thông tin sản phẩm
                var productDetails = await GetProductDetailsAsync(listItemCartDTo.cartDTos);
                // Tính giá tạm tính sau khi đã có thông tin sản phẩm
                var totalPrice = productDetails.Sum(p => p.SellPrice * p.Quantity);
                ViewBag.totalPrice = totalPrice;

                var result = Tuple.Create(productDetails, user_return);

                return View(result);
            }
            return NotFound("Không có sản phẩm nào trong giỏ hàng.");
        }
        [HttpPost("Checkout")]
        public async Task<IActionResult> Checkout(PaymentDTo model, string paymentMethod)
        {
            if (!ModelState.IsValid)
            {
                return NotFound("Đã có lỗi khi nhận thông tin, hãy thông báo với Admin");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                return Unauthorized("User không hợp lệ");
            }

            if (paymentMethod=="momo")
            { 
                var userInfo = await _context.Users.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
                var totalAmount = GetTotalPrice(model);
                var momoPayModel = new MomoPaymentResquestModel
                {
                    Amount = (double)totalAmount,
                    CreatedDate = DateTime.Now,
                    Description = "Đơn hàng",
                    FullName = userInfo.LastName + " " + userInfo.FirstName,
                    OrderId = new Random().Next(1000, 10000)
                };
                // Lưu trữ thông tin vào session
                HttpContext.Session.SetObject("PaymentInfo", model);

                var momoPayUrl = await _momoService.CreatePaymentUrl(momoPayModel);
                return Json(new { momoPayUrl = momoPayUrl.PayUrl });
            }
            if (paymentMethod == "vnpay")
            {
                var userInfo = await _context.Users.FirstOrDefaultAsync(x=>x.UserId == int.Parse(userId));
                var totalAmount = GetTotalPrice(model);
                var vnPayModel = new VnPaymentResquestModel
                {
                    Amount = (double)totalAmount,
                    CreatedDate = DateTime.Now,
                    Description = "Đơn hàng",
                    FullName = userInfo.LastName + userInfo.FirstName,
                    OrderId = new Random().Next(1000,10000)
                };
                // Lưu trữ thông tin vào session
                HttpContext.Session.SetObject("PaymentInfo", model);

                var vnPayUrl = _vnPayService.CreatePaymentUrl(HttpContext, vnPayModel);
                return Json(new { vnPayUrl });
            }
            if (paymentMethod == "cod")
            {
                var user = await _context.Users.FindAsync(int.Parse(userId));
                if (user == null)
                {
                    return NotFound("Không tìm thấy người dùng");
                }

                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    Address address = null;

                    // Thêm địa chỉ mới nếu cần
                    if (model.NewAddress)
                    {
                        address = await AddShipAddress(model, int.Parse(userId));
                    }
                    else
                    {
                        address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
                    }

                    // Kiểm tra nếu không có địa chỉ
                    if (address == null)
                    {
                        return NotFound("Không tìm thấy địa chỉ vận chuyển.");
                    }

                    // Tính tổng số tiền
                    var totalAmount = GetTotalPrice(model);
                    var originAmount = GetOriginPrice(model);
                    decimal discountPrice = 0;
                    if (!string.IsNullOrEmpty(model.VoucherCode))
                    {
                        var voucher = await _context.Vouchers.Select(x => new { x.Promotion, x.Code }).FirstOrDefaultAsync(x => x.Code == model.VoucherCode);
                        discountPrice = ParseCurrency(voucher.Promotion, totalAmount);
                    }
                    // Tạo đơn hàng
                    var order = new Order
                    {
                        UserId = int.Parse(userId),
                        OrderDate = DateTime.Now,
                        TotalAmount = totalAmount,
                        OriginAmount = originAmount,
                        Note = model.Note,
                        DiscountAmount = discountPrice,
                        OrderStatus = "Pending",
                        ShippingAmount = 30000,//Chưa xử dụng API giao hàng nên đặt tạm là 30k/đơn hàng
                        ShippingAddressId = address.AddressId,  // Gán AddressId từ địa chỉ đã tìm thấy hoặc vừa thêm
                    };

                    // Thêm đơn hàng vào cơ sở dữ liệu
                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();


                    // Thêm các mục vào OrderItem
                    var orderItems = model.Products.Select(item => new OrderItem
                    {
                        OrderId = order.OrderId,
                        VarientProductId = item.VarientProductID,
                        ProductId = item.ProductId,
                        Quantity = item.Quantity,
                        Price = _context.VarientProducts
                                   .Where(x => x.VarientId == item.VarientProductID)
                                   .Select(v => v.Price ?? v.Product.SellPrice)
                                   .FirstOrDefault() ?? 0
                    }).ToList();

                    _context.OrderItems.AddRange(orderItems);

                    // Thêm thanh toán
                    var payment = new Payment
                    {
                        OrderId = order.OrderId,
                        PaymentDate = DateTime.Now,
                        PaymentMethod = "COD",
                        Status = "Unpaid",
                        Amount = totalAmount
                    };
                    _context.Payments.Add(payment);

                    // Trừ số lượng voucher nếu có
                    if (!string.IsNullOrEmpty(model.VoucherCode))
                    {
                        var voucher_get = await _context.Vouchers
                            .FirstOrDefaultAsync(x => x.Code == model.VoucherCode);

                        if (voucher_get != null && voucher_get.Quantity > 0)
                        {
                            voucher_get.Quantity -= 1;
                            _context.Vouchers.Update(voucher_get);
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    //Tạo thông báo cho ADMIN và Client

                    try
                    {
                        await _notificationService.NotifyAsync(NotificationTarget.Admins, "Xác nhận đơn hàng",
                            $"Có đơn hàng mới từ người dùng {user.Email} !", "new order", $"/admin/orders/view/{order.OrderId}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi gửi thông báo cho Admin: {ex.Message}");
                    }

                    try
                    {
                        await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Xác nhận đơn hàng",
                            "Đơn hàng của bạn đang được xử lý.", "success", $"/user/MyOrders/OrderDetail/{order.OrderId}",
                            new List<int> { user?.UserId ?? 0 });  // Dùng null-coalescing để tránh lỗi null
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Lỗi khi gửi thông báo cho User: {ex.Message}");
                    }

                    return Json(new { success = true, redirectTo = Url.Action("OrderSuccess") });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return StatusCode(500, "Có lỗi xảy ra trong quá trình xử lý đơn hàng.");
                }
            }

            return BadRequest("Phương thức thanh toán không hợp lệ.");
        }
        // Hàm hỗ trợ để phân tích giá trị tiền tệ
        private decimal ParseCurrency(string input, decimal totalPrice)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return 0; // Nếu không có giá trị, trả về 0
            }

            // Kiểm tra xem giá trị có phải là phần trăm hay không
            if (input.EndsWith("%"))
            {
                // Xử lý phần trăm
                decimal percentage = decimal.Parse(input.TrimEnd('%')) / 100;
                return totalPrice * percentage; // Trả về giá trị tính từ phần trăm dựa trên totalPrice
            }

            // Nếu là giá trị tiền tệ, loại bỏ ký tự không cần thiết và chuyển đổi
            input = input.Replace("₫", "").Replace(",", "").Trim(); // Xóa ký tự ₫ và dấu phẩy
            return decimal.Parse(input); // Chuyển đổi chuỗi thành decimal
        }
        // Hàm tính tổng giá theo ID voucher và Giá của cả cart
        private decimal GetTotalPrice(PaymentDTo model)
        {
            decimal voucherAmount = 0;
            decimal tempAmount = 0;

            foreach (var item in model.Products)
            {
                var varient = _context.VarientProducts
                    .Where(x => x.VarientId == item.VarientProductID)
                    .Select(v => v.Price ?? v.Product.SellPrice)
                    .FirstOrDefault();

                if (varient.HasValue)
                {
                    tempAmount += varient.Value * item.Quantity;
                }
            }

            if (!string.IsNullOrEmpty(model.VoucherCode))
            {
                var voucher = _context.Vouchers.FirstOrDefault(x => x.Code == model.VoucherCode);
                if (voucher != null)
                {
                    if (voucher.Promotion.EndsWith("%"))
                    {
                        var percentage = int.Parse(voucher.Promotion.TrimEnd('%'));
                        voucherAmount = (tempAmount / 100) * percentage;
                    }
                    else
                    {
                        voucherAmount = decimal.Parse(voucher.Promotion);
                    }
                }
            }

            return tempAmount - voucherAmount;
        }
        //Hàm lấy g ía trị gốc của hóa đơn
        private decimal GetOriginPrice(PaymentDTo model)
        {
            decimal originAmmount = 0;

            foreach (var item in model.Products)
            {
                var varient = _context.VarientProducts
                    .Where(x => x.VarientId == item.VarientProductID)
                    .Select(v => v.Price ?? v.Product.SellPrice)
                    .FirstOrDefault();

                if (varient.HasValue)
                {
                    originAmmount += varient.Value * item.Quantity;
                }
            }
            return originAmmount;
        }

        // Hàm thêm địa chỉ Ship khác so với địa chỉ mặc định
        private async Task<Address> AddShipAddress(PaymentDTo paymentDTo, int userId)
        {
            var address = new Address
            {
                UserId = userId,
                AddressLine = paymentDTo.Address.AddressLine,
                Ward = paymentDTo.Address.Ward,
                District = paymentDTo.Address.District,
                Province = paymentDTo.Address.Province,
            };
            _context.Addresses.Add(address);
            await _context.SaveChangesAsync();
            return address; // Trả về Address vừa thêm
        }
        [Route("MoMoPayCallBack")]
        public async Task<IActionResult> MoMoPayCallBack()
        {
            var response = _momoService.PaymentExecute(Request.Query);
            var requestQuery = HttpContext.Request.Query;
            if (response == null /*|| requestQuery["resultCode"] != 0*/)
            {
                TempData["Message"] = $"Lỗi thanh toán MoMo: {requestQuery["resultCode"]}";
                return RedirectToAction("PaymentFail");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User không hợp lệ");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng");
            }

            // Bắt đầu transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy thông tin từ session hoặc TempData (cần lưu từ bước Checkout)
                var paymentInfo = HttpContext.Session.GetObject<PaymentDTo>("PaymentInfo");
                if (paymentInfo == null)
                {
                    return BadRequest("Không tìm thấy thông tin thanh toán");
                }

                var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
                if (paymentInfo.NewAddress)
                {
                    address = await AddShipAddress(paymentInfo, int.Parse(userId));
                }

                if (address == null)
                {
                    return NotFound("Không tìm thấy địa chỉ vận chuyển.");
                }

                // Tính toán giá tiền
                var totalAmount = GetTotalPrice(paymentInfo);
                var originAmount = GetOriginPrice(paymentInfo);
                decimal discountPrice = 0;

                if (!string.IsNullOrEmpty(paymentInfo.VoucherCode))
                {
                    var voucher = await _context.Vouchers
                        .Select(x => new { x.Promotion, x.Code })
                        .FirstOrDefaultAsync(x => x.Code == paymentInfo.VoucherCode);
                    discountPrice = ParseCurrency(voucher.Promotion, totalAmount);
                }

                // Tạo đơn hàng mới
                var order = new Order
                {
                    UserId = int.Parse(userId),
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    OriginAmount = originAmount,
                    Note = paymentInfo.Note,
                    DiscountAmount = discountPrice,
                    OrderStatus = "Confirmed", // Đơn hàng đã được xác nhận vì đã thanh toán
                    ShippingAmount = 30000,
                    ShippingAddressId = address.AddressId,
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Thêm chi tiết đơn hàng
                var orderItems = paymentInfo.Products.Select(item => new OrderItem
                {
                    OrderId = order.OrderId,
                    VarientProductId = item.VarientProductID,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = _context.VarientProducts
                        .Where(x => x.VarientId == item.VarientProductID)
                        .Select(v => v.Price ?? v.Product.SellPrice)
                        .FirstOrDefault() ?? 0
                }).ToList();

                _context.OrderItems.AddRange(orderItems);

                // Tạo bản ghi thanh toán
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "MoMo",
                    Status = "Paid", // Đã thanh toán thành công
                    Amount = totalAmount,
                    //TransactionNo = response.TransactionNo, // Lưu mã giao dịch VNPay
                    //PaymentTime = response.PaymentTime // Thời gian thanh toán từ VNPay
                };

                _context.Payments.Add(payment);

                // Cập nhật số lượng voucher nếu có sử dụng
                if (!string.IsNullOrEmpty(paymentInfo.VoucherCode))
                {
                    var voucher = await _context.Vouchers
                        .FirstOrDefaultAsync(x => x.Code == paymentInfo.VoucherCode);

                    if (voucher != null && voucher.Quantity > 0)
                    {
                        voucher.Quantity -= 1;
                        _context.Vouchers.Update(voucher);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Xóa thông tin thanh toán khỏi session
                HttpContext.Session.Remove("PaymentInfo");

                await _notificationService.NotifyAsync(NotificationTarget.Admins, "Thanh toán thành công", $"Đơn hàng {order.OrderId} đã được thanh toán thành công !", "payment received", $"/admin/transactions");

                await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Thanh toán thành công", $"Đơn hàng {order.OrderId} đã được thanh toán thành công !", "success", $"/user/MyOrders/OrderDetail/{order.OrderId}", new List<int> { user.UserId });


                TempData["Message"] = "Thanh toán VNPay thành công";
                return RedirectToAction("PaymentSuccess");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log exception
                return StatusCode(500, "Có lỗi xảy ra trong quá trình xử lý đơn hàng.");
            }
        }

        [Route("VnPayCallBack")]
        public async Task<IActionResult> VnPayCallBack()
        {
            var response = _vnPayService.PaymentExecute(Request.Query);
            if (response == null || response.VnPayResponseCode != "00")
            {
                TempData["Message"] = $"Lỗi thanh toán VNPay: {response.VnPayResponseCode}";
                return RedirectToAction("PaymentFail");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Unauthorized("User không hợp lệ");
            }

            var user = await _context.Users.FindAsync(int.Parse(userId));
            if (user == null)
            {
                return NotFound("Không tìm thấy người dùng");
            }

            // Bắt đầu transaction
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Lấy thông tin từ session hoặc TempData (cần lưu từ bước Checkout)
                var paymentInfo = HttpContext.Session.GetObject<PaymentDTo>("PaymentInfo");
                if (paymentInfo == null)
                {
                    return BadRequest("Không tìm thấy thông tin thanh toán");
                }

                var address = await _context.Addresses.FirstOrDefaultAsync(x => x.UserId == int.Parse(userId));
                if (paymentInfo.NewAddress)
                {
                    address = await AddShipAddress(paymentInfo, int.Parse(userId));
                }

                if (address == null)
                {
                    return NotFound("Không tìm thấy địa chỉ vận chuyển.");
                }

                // Tính toán giá tiền
                var totalAmount = GetTotalPrice(paymentInfo);
                var originAmount = GetOriginPrice(paymentInfo);
                decimal discountPrice = 0;

                if (!string.IsNullOrEmpty(paymentInfo.VoucherCode))
                {
                    var voucher = await _context.Vouchers
                        .Select(x => new { x.Promotion, x.Code })
                        .FirstOrDefaultAsync(x => x.Code == paymentInfo.VoucherCode);
                    discountPrice = ParseCurrency(voucher.Promotion, totalAmount);
                }

                // Tạo đơn hàng mới
                var order = new Order
                {
                    UserId = int.Parse(userId),
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    OriginAmount = originAmount,
                    Note = paymentInfo.Note,
                    DiscountAmount = discountPrice,
                    OrderStatus = "Confirmed", // Đơn hàng đã được xác nhận vì đã thanh toán
                    ShippingAmount = 30000,
                    ShippingAddressId = address.AddressId,
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Thêm chi tiết đơn hàng
                var orderItems = paymentInfo.Products.Select(item => new OrderItem
                {
                    OrderId = order.OrderId,
                    VarientProductId = item.VarientProductID,
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = _context.VarientProducts
                        .Where(x => x.VarientId == item.VarientProductID)
                        .Select(v => v.Price ?? v.Product.SellPrice)
                        .FirstOrDefault() ?? 0
                }).ToList();

                _context.OrderItems.AddRange(orderItems);

                // Tạo bản ghi thanh toán
                var payment = new Payment
                {
                    OrderId = order.OrderId,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "VNPay",
                    Status = "Paid", // Đã thanh toán thành công
                    Amount = totalAmount,
                    //TransactionNo = response.TransactionNo, // Lưu mã giao dịch VNPay
                    //PaymentTime = response.PaymentTime // Thời gian thanh toán từ VNPay
                };

                _context.Payments.Add(payment);

                // Cập nhật số lượng voucher nếu có sử dụng
                if (!string.IsNullOrEmpty(paymentInfo.VoucherCode))
                {
                    var voucher = await _context.Vouchers
                        .FirstOrDefaultAsync(x => x.Code == paymentInfo.VoucherCode);

                    if (voucher != null && voucher.Quantity > 0)
                    {
                        voucher.Quantity -= 1;
                        _context.Vouchers.Update(voucher);
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                // Xóa thông tin thanh toán khỏi session
                HttpContext.Session.Remove("PaymentInfo");

                TempData["Message"] = "Thanh toán VNPay thành công";

                await _notificationService.NotifyAsync(NotificationTarget.Admins, "Thanh toán thành công", $"Đơn hàng {order.OrderId} đã được thanh toán thành công !", "payment received", $"/admin/transactions");

                await _notificationService.NotifyAsync(NotificationTarget.SpecificUsers, "Thanh toán thành công", $"Đơn hàng {order.OrderId} đã được thanh toán thành công !", "success", $"/user/MyOrders/OrderDetail/{order.OrderId}", new List<int> { user.UserId });

                return RedirectToAction("PaymentSuccess");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                // Log exception
                return StatusCode(500, "Có lỗi xảy ra trong quá trình xử lý đơn hàng.");
            }
        }
        [Route("PaymentSuccess")]
        public IActionResult PaymentSuccess() { return View(); }

        [Route("PaymentFail")]
        public IActionResult PaymentFail() { return View(); }
        [Route("OrderSuccess")]
        public IActionResult OrderSuccess() { return View(); } 
        //phương thức để lấy chi tiết sản phẩm
        private async Task<List<CheckoutDTo>> GetProductDetailsAsync(List<CartDTo> cartItems)
        {
         
            var productDetails = new List<CheckoutDTo>();
          
            foreach (var item in cartItems)
            {
                var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == item.ProductId);
                var varientProduct = await _context.VarientProducts.FirstOrDefaultAsync(x => x.VarientId == item.VarientProductId);
                if (product != null)
                {
                    productDetails.Add(new CheckoutDTo
                    {
                        ProductId = product.ProductId,
                        ProductName = product.Name,
                        ImageUrl = product.Image,
                        Quantity = item.Quantity,
                        VarientId = item.VarientProductId,
                        SellPrice = varientProduct.Price ?? 0,
                        OriginPrice = product.OriginalPrice,
                        Attributes = varientProduct.Attributes,
                    });
                }
            }

            return productDetails;
        }
        // Phương thức để tính giá tạm tính
        private async Task<decimal> CalculateTotalPriceAsync(List<CartDTo> cartItems)
        {
            var productDetails = await GetProductDetailsAsync(cartItems);
            decimal subtotal = 0;

            foreach (var product in productDetails)
            {
                subtotal += product.SellPrice * product.Quantity;
            }

            return subtotal;
        }

        [HttpGet("CheckVoucher")]
        public async Task<JsonResult> CheckVoucher(string code)
        {
            var voucher = await _context.Vouchers.FirstOrDefaultAsync(x => x.Code.ToLower().Trim() == code.ToLower().Trim());

            if (voucher == null)
            {
                return Json(new { success = false, message = "Voucher không đúng" });
            }
            else
            {
                // Chuyển DateTime.Now thành DateOnly
                var today = DateOnly.FromDateTime(DateTime.Now);

                // Kiểm tra nếu voucher chưa bắt đầu
                if (voucher.StartedAt.HasValue && voucher.StartedAt.Value > today)
                {
                    return Json(new { success = false, message = "Voucher chưa đến thời hạn sử dụng" });
                }

                // Kiểm tra nếu voucher đã hết hạn
                if (voucher.ExpiredAt.HasValue && voucher.ExpiredAt.Value < today)
                {
                    return Json(new { success = false, message = "Voucher đã hết hạn" });
                }
                //Kiểm tra nếu SL voucher đã hết 
                if (voucher.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Voucher đã hết" });
                }
            }

            return Json(new { success = true,message="Voucher đã được áp dụng", voucher });
        }

    }
}
