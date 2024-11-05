using Microsoft.AspNetCore.Mvc;
using Microsoft.DotNet.Scaffolding.Shared.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Newtonsoft.Json;
using SelectPdf;
using System.Globalization;
using System.Text;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using ZXing.QrCode.Internal;
using static Tech_Store.Models.DTO.Province;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class POSController : BaseAdminController
    {
        public POSController(ApplicationDbContext context) : base(context) { }
    
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            var products = _context.Products.OrderByDescending(x => x.ProductId).ToList();
            var users = _context.Users.Where(x=>x.FirstName!=null&& x.LastName!=null).OrderByDescending(x => x.UserId).ToList();

            ViewBag.category = categories;
            ViewBag.product = products;
            ViewBag.users = users;
            return View();
        }

        [Route("Invoice/{id}")]
        public async Task<IActionResult> Invoice(int id)
        {
            var order = await _context.Orders
              .Include(x => x.OrderItems)
                  .ThenInclude(oi => oi.Product)
                      .ThenInclude(p => p.VarientProducts) // Include VariantProduct from Product
              .Include(x => x.Payments).Include(x=>x.ShippingAddress)
              .Include(x => x.User)
              .FirstOrDefaultAsync(x => x.OrderId == id);

            if (order == null)
            {
                return BadRequest("Không tìm thấy");
            }

            var payment = await _context.Payments.FirstOrDefaultAsync(x => x.OrderId == id);
            ViewBag.Payment = payment;

            // Read the JSON file
            var jsonString = await System.IO.File.ReadAllTextAsync("wwwroot/Province_VN.json");
            var provinces = JsonConvert.DeserializeObject<List<Province>>(jsonString);

            // Find the province, district, and ward by ID
            var province = provinces?.FirstOrDefault(p => p.Code == int.Parse(order.ShippingAddress.Province));
            var district = province?.Districts?.FirstOrDefault(d => d.Code == int.Parse(order.ShippingAddress.District));
            var ward = district?.Wards?.FirstOrDefault(w => w.Code == int.Parse(order.ShippingAddress.Ward));

            // Assign to ViewBag
            ViewBag.Address = $"{order.ShippingAddress.AddressLine},{ward?.Name}, {district?.Name}, {province?.Name}";

            return View(order);
        }


        [HttpGet]
        [Route("GetUser")]
        public async Task<JsonResult> GetUser(int id)
        {
            var cus = await _context.Users.Include(x => x.Addresses).Select(x=> new {x.UserId,x.LastName,x.FirstName,x.PhoneNumber,x.Addresses})
                .FirstOrDefaultAsync(x => x.UserId == id);
            if (cus == null)
            {
                return Json(new { success = false, message = "Không tìm thấy người dùng này" });
            }
            return Json(cus);
        }
        [HttpGet]
        [Route("CheckVoucher")]
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
                if(voucher.Quantity <= 0)
                {
                    return Json(new { success = false, message = "Voucher đã hết" });
                }
            }

            return Json(new { success = true, voucher });
        }

        [HttpGet]
        [Route("GetProduct")]
        public async Task<JsonResult> GetProduct(int id)
        {
            var product = await _context.Products.Include(x=>x.VarientProducts).Include(x=>x.Brand).Include(x=>x.Category)
                .Select(x => new
                {
                    x.ProductId,
                    x.Name,
                    SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                    OriginalPrice = x.OriginalPrice.HasValue ? x.OriginalPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                    x.Description,
                    x.Image,
                    x.Stock,
                    x.Sku,
                    x.VarientProducts,
                    x.Brand,
                    x.Category
                })
                .FirstOrDefaultAsync(x => x.ProductId == id);

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không hợp lệ" });
            }

            // Chỉ trả về những thông tin cần thiết
            return Json(new
            {
                success = true,
                product
            });
        }
        [HttpGet]
        [Route("GetProductsByCategory")]
        public async Task<JsonResult> GetProductsByCategory(int cateId)
        {
            // Nếu cateId == null, lấy tất cả sản phẩm, ngược lại chỉ lấy sản phẩm thuộc cateId
            var products = await _context.Products
                .Select(x => new
                {
                    x.ProductId,
                    x.Name,
                    SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                    x.Description,
                    x.Image
                })
               .ToListAsync();
            if(cateId != 0)
            {
                products = await _context.Products
                  .Where(x => x.CategoryId == cateId)// Nếu cateId không có giá trị, lấy tất cả sản phẩm
               .Select(x => new
               {
                   x.ProductId,
                   x.Name,
                   SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                   x.Description,
                   x.Image
               })
               .ToListAsync();
            }
            // Kiểm tra xem có sản phẩm nào không
            if (products == null)
            {
                return Json(new { success = false, message = "Không tìm thấy sản phẩm phù hợp" });
            }

            return Json(new
            {
                success = true,
                products
            });
        }
        [HttpGet]
        [Route("GetProducts")]
        public async Task<JsonResult> GetProducts(string name, int cateId)
        {
            IQueryable<Product> query = _context.Products;

            // Apply category filter if cateId is provided
            if (cateId > 0)
            {
                query = query.Where(x => x.CategoryId == cateId);
            }

            // Apply name filter if name is provided
            if (!string.IsNullOrWhiteSpace(name))
            {
                query = query.Where(x => x.Name.Contains(name));
            }

            var products = await query.Select(x => new
            {
                x.ProductId,
                x.Name,
                SellPrice = x.SellPrice.HasValue ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN")) : null,
                x.Description,
                x.Image
            }).OrderByDescending(x => x.ProductId).ToListAsync();

            return Json(new
            {
                success = true,
                products
            });
        }
        [HttpGet]
        [Route("GetVarientProduct")]
        public async Task<JsonResult> GetVarientProduct(int id,int productId)
        {
            var varient = _context.VarientProducts.Select(x => new { x.VarientId,x.Sku,x.Attributes,x.Price,x.Stock,x.Product}).
                FirstOrDefaultAsync(x=>x.VarientId == id && x.Product.ProductId == productId);
            if(varient == null)
            {
                return Json(new { success = false, message = "Không tìm thấy biến thể" });
            }
            return Json(new { success = true, varient });
        }
        [HttpPost]
        [Route("AddToCart")]
        public async Task<JsonResult> AddToCard([FromBody]CartDTo cart)
        {
            if (cart == null)
            {
                return Json(new { success = false, message = "Đã có lỗi khi lấy thông tin" });
            }

            var varient = await _context.VarientProducts
                .Include(v => v.Product) // Bao gồm thông tin sản phẩm
                .FirstOrDefaultAsync(x => x.VarientId == cart.VarientProductId);

            if (varient == null)
            {
                return Json(new { success = false, message = "Đã có lỗi khi lấy thông tin sản phẩm" });
            }

            if (varient.Stock < cart.Quantity)
            {
                return Json(new { success = false, message = "Sản phẩm không có đủ" });
            }

            var price = varient.Price * cart.Quantity;
            var formattedPrice = Math.Round((decimal)price, 0).ToString("N0");
            // Tạo HTML cho một dòng mới trong bảng
            var newRow = $@"
            <tr data-varient-id='{varient.VarientId}'>
               <td class='align-items-center'>
                      <img src=""/Upload/Products/{varient.Product.Image}""  class=""img-thumbnail""  style=""height:35px;width:35px;object-fit: cover;"" />
                       <span class='text-truncate'>{varient.Product.Name}</span> 
                </td>
                </td>

                <td><span class='text-truncate'>{varient.Attributes}</span></td>
                <td>
                   <input min=""1"" max=""{varient.Stock}"" type=""number"" class=""form-control quantity-input"" data-product-id=""{cart.ProductId}"" value=""{cart.Quantity}"" />

                </td>
                <td class='price-cell' data-price='{varient.Price}'>{formattedPrice}đ</td>
                <td>
                    <button class='btn btn-danger btn-sm btn-delete'>
                        <i class='fas fa-trash'></i>
                    </button>
                </td>
            </tr>";


            return Json(new { success = true, result = newRow });
        }
        [HttpPost]
        [Route("Update-Quantity")]
        public async Task<JsonResult> UpdateQuantity([FromBody] CartDTo cart)
        {
            if (cart == null)
            {
                return Json(new { success = false, message = "Đã có lỗi khi lấy thông tin" });
            }

            var varient = await _context.VarientProducts
                .Include(v => v.Product) // Bao gồm thông tin sản phẩm
                .FirstOrDefaultAsync(x => x.VarientId == cart.VarientProductId);

            if (varient == null)
            {
                return Json(new { success = false, message = "Đã có lỗi khi lấy thông tin sản phẩm" });
            }

            // Kiểm tra số lượng trong kho
            if (varient.Stock < cart.Quantity)
            {
                return Json(new { success = false, message = "Sản phẩm không có đủ trong kho" });
            }

            // Tính lại giá dựa trên số lượng mới
            var price = varient.Price * cart.Quantity;
            var formattedPrice = Math.Round((decimal)price, 0).ToString("N0");

            // Tạo HTML cho dòng sản phẩm với số lượng cập nhật
            var newRow = $@"
            <tr data-varient-id='{varient.VarientId}'>
               <td class='align-items-center'>
                      <img src=""/Upload/Products/{varient.Product.Image}""  class=""img-thumbnail""  style=""height:35px;width:35px;object-fit: cover;"" />
                       <span class='text-truncate'>{varient.Product.Name}</span> 
                </td>
                </td>

                <td><span class='text-truncate'>{varient.Attributes}</span></td>
                <td>
                   <input min=""1"" max=""{varient.Stock}"" type=""number"" class=""form-control quantity-input"" data-product-id=""{cart.ProductId}"" value=""{cart.Quantity}"" />

                </td>
                <td class='price-cell' data-price='{varient.Price}'>{formattedPrice}đ</td>
                <td>
                    <button class='btn btn-danger btn-sm btn-delete'>
                        <i class='fas fa-trash'></i>
                    </button>
                </td>
            </tr>";

            return Json(new { success = true, result = newRow });
        }


        private decimal calculatePrice(decimal Price,int quantity)
        {
            return Price * quantity;
        }
        [HttpPost]
        [Route("Order")]
        public async Task<JsonResult> Order([FromBody] InvoicesDTo invoice)
        {
            if (ModelState.IsValid)
            {
                // Start a new transaction
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Kiểm tra địa chỉ giao hàng
                    var address = await _context.Addresses
                        .Where(x => x.UserId == invoice.UserId)
                        .Select(x => new { x.AddressId, x.UserId })
                        .FirstOrDefaultAsync();

                    if (address == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy địa chỉ giao hàng" });
                    }

                    // Tạo đơn hàng
                    var order = new Order
                    {
                        UserId = invoice.UserId,
                        TotalAmount = invoice.TotalPrice,
                        ShippingAddressId = address.AddressId,
                        OrderDate = DateTime.Now, // Ghi nhận thời gian tạo đơn hàng
                        OrderStatus = "Pending" // Trạng thái đơn hàng ban đầu
                    };

                    _context.Orders.Add(order);
                    await _context.SaveChangesAsync();

                    foreach (var varient in invoice.ListVarientProduct)
                    {
                        // Lấy toàn bộ đối tượng VarientProduct
                        var varientproduct = await _context.VarientProducts
                            .FirstOrDefaultAsync(x => x.VarientId == varient.VarientProductId);

                        if (varientproduct == null /*|| product.Stock < invoice.Quantities[varientId]*/)
                        {
                            // Rollback the transaction and return an error
                            await transaction.RollbackAsync();
                            return Json(new { success = false, message = $"Sản phẩm với biến thể ID {varient} không đủ hàng." });
                        }

                        // Tạo OrderItem
                        var orderItem = new OrderItem
                        {
                            OrderId = order.OrderId,
                            ProductId = varientproduct.ProductId,
                            VarientProductId = varient.VarientProductId,
                            Quantity = varient.Quantity,
                            Price = (decimal)varientproduct.Price
                        };

                        _context.OrderItems.Add(orderItem);

                        // Cập nhật số lượng sản phẩm trong kho
                        var product = await _context.Products.FirstOrDefaultAsync(x => x.ProductId == varientproduct.ProductId);
                        product.Stock -= varient.Quantity;
                        varientproduct.Stock -= varient.Quantity;
                        _context.VarientProducts.Update(varientproduct);
                    }

                    // Lưu thay đổi cho OrderItems và cập nhật kho hàng
                    await _context.SaveChangesAsync();

                    // Xử lý thanh toán
                    decimal deductPrice = ParseCurrency(invoice.DeductPrice,invoice.TotalPrice);
                    decimal discountPrice = ParseCurrency(invoice.DiscountPrice,invoice.TotalPrice);
                    var payment = new Payment
                    {
                        OrderId = order.OrderId,
                        PaymentMethod = invoice.PaymentMethod,
                        Amount = invoice.TotalPrice,
                        OriginAmount= invoice.OriginTotalPrice,
                        Status = "Paid", // Đánh dấu thanh toán thành công
                        DiscountAmount = discountPrice,
                        DeductAmount = deductPrice,
                    };
                    _context.Payments.Add(payment);

                    // Trừ số lượng voucher nếu có
                    if (!string.IsNullOrEmpty(invoice.Voucher))
                    {
                        var voucher = await _context.Vouchers
                            .FirstOrDefaultAsync(x => x.Code == invoice.Voucher);

                        if (voucher != null && voucher.Quantity > 0)
                        {
                            voucher.Quantity -= 1;
                            _context.Vouchers.Update(voucher);
                        }
                    }

                    // Lưu tất cả thay đổi
                    await _context.SaveChangesAsync();

                    // Commit the transaction
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Đặt hàng Thành công",id = order.OrderId });
                }
                catch (Exception ex)
                {
                    // Rollback the transaction in case of any error
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Có lỗi xảy ra, đơn hàng không được tạo", error = ex.Message });
                }
            }
            else
            {
                return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
            }
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
        [HttpGet]
        [Route("Print-Invoice")]
        public async Task<IActionResult> PrintInvoice(int id)
        {
            var invoice = await _context.Orders
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.VarientProducts) // Include VariantProduct from Product
                .Include(x => x.Payments).Include(x => x.ShippingAddress)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrderId == id);

            if (invoice == null)
            {
                return NotFound();
            }
            var payment = await _context.Payments.FirstOrDefaultAsync(x => x.OrderId == id);

            // Tạo tệp PDF
            string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Template", "invoice.html");
            string htmlTemplate = await System.IO.File.ReadAllTextAsync(templatePath);

            // Thay thế các giá trị trong template
            htmlTemplate = htmlTemplate.Replace("{{name}}", invoice.User.LastName + " " + invoice.User.FirstName);
            htmlTemplate = htmlTemplate.Replace("{{address}}", invoice.ShippingAddress.AddressLine);
            htmlTemplate = htmlTemplate.Replace("{{address}}", invoice.ShippingAddress.AddressLine);
            htmlTemplate = htmlTemplate.Replace("{{phoneNumber}}", invoice.User.PhoneNumber);
            htmlTemplate = htmlTemplate.Replace("{{orderID}}", invoice.OrderId.ToString());
            htmlTemplate = htmlTemplate.Replace("{{orderDate}}", invoice.OrderDate?.ToString("dd/MM/yyyy"));
            htmlTemplate = htmlTemplate.Replace("{{oderStatus}}", invoice.OrderStatus);
            htmlTemplate = htmlTemplate.Replace("{{originAmount}}", payment.OriginAmount?.ToString("C0", new CultureInfo("vi-VN")));
            htmlTemplate = htmlTemplate.Replace("{{discountAmount}}", payment.DiscountAmount?.ToString("C0", new CultureInfo("vi-VN")));
            htmlTemplate = htmlTemplate.Replace("{{deductAmount}}", payment.DeductAmount?.ToString("C0", new CultureInfo("vi-VN")));
            htmlTemplate = htmlTemplate.Replace("{{amount}}", payment.Amount.ToString("C0", new CultureInfo("vi-VN")));

            // Thay thế chi tiết sản phẩm
            var productRows = new StringBuilder();
            int index = 1;
            foreach (var item in invoice.OrderItems)
            {
                productRows.Append($"<tr><th scope='row'>{index++}</th><td>{item.Product.Name} ({item.VarientProduct.Attributes})</td><td>{item.Quantity}</td><td>{item.Price:C}</td></tr>");
            }
            htmlTemplate = htmlTemplate.Replace("{{productRows}}", productRows.ToString());

            // Tạo PDF từ HTML
            var converter = new SelectPdf.HtmlToPdf();
            var doc = converter.ConvertHtmlString(htmlTemplate);

            // Trả về PDF để tải xuống
            byte[] pdf = doc.Save();
            doc.Close();

            return File(pdf, "application/pdf", $"invoice_{id}.pdf");

        }
    }
}
