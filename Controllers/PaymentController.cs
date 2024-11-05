using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Security.Claims;
using Tech_Store.Models;
using Tech_Store.Models.DTO;

namespace Tech_Store.Controllers
{
    [Route("Payment")]
    public class PaymentController : BaseController
    {
        public PaymentController(ApplicationDbContext _context):base(_context) { }


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
