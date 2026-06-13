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
using Tech_Store.Models.DTO.Payment.Admin;
using Tech_Store.Helpers;
using Tech_Store.Services;
using Tech_Store.Services.Admin.NotificationServices;
using ZXing.QrCode.Internal;
using static Tech_Store.Models.DTO.Province;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("admin/[controller]")]
    public class POSController : BaseAdminController
    {
        private readonly NotificationService _notificationService;
        private readonly IEmailService _emailService;
        public POSController(ApplicationDbContext context, NotificationService notificationService, IEmailService emailService) : base(context) {
            _notificationService = notificationService;
            _emailService = emailService;
        }
    
        [Route("")]
        [Route("Index")]
        public IActionResult Index()
        {
            var categories = _context.Categories.ToList();
            var products = _context.Products
                 .Include(x => x.VarientProducts)
                 .ThenInclude(vp => vp.VariantAttributes)
                     .ThenInclude(a => a.AttributeValue)
                     .Where(x=>x.Status != "outstock" && x.Status != "discontinued")
                .OrderByDescending(x => x.ProductId).Take(50).ToList();

            ViewBag.category = categories;
            ViewBag.product = products;
            return View();
        }

        [HttpGet]
        [Route("GetCustomers")]
        public async Task<JsonResult> GetCustomers(string? keyword, int page = 1, int pageSize = 10)
        {
            var resolvedPage = page < 1 ? 1 : page;
            var resolvedPageSize = pageSize <= 0 ? 10 : Math.Min(pageSize, 50);

            var query = _context.Users
                .AsNoTracking()
                .Select(x => new
                {
                    x.UserId,
                    x.FirstName,
                    x.LastName,
                    x.PhoneNumber,
                    x.Email,
                    Address = x.Addresses
                        .OrderByDescending(a => a.UpdatedAt ?? a.CreatedAt)
                        .Select(a => new
                        {
                            a.AddressLine,
                            a.Ward,
                            a.District,
                            a.Province
                        })
                        .FirstOrDefault()
                });

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    ((x.LastName ?? string.Empty) + " " + (x.FirstName ?? string.Empty)).Contains(normalizedKeyword) ||
                    (x.PhoneNumber ?? string.Empty).Contains(normalizedKeyword) ||
                    (x.Email ?? string.Empty).Contains(normalizedKeyword));
            }

            var totalItems = await query.CountAsync();
            var totalPages = totalItems == 0 ? 1 : (int)Math.Ceiling(totalItems / (double)resolvedPageSize);
            if (resolvedPage > totalPages)
            {
                resolvedPage = totalPages;
            }

            var customers = await query
                .OrderByDescending(x => x.UserId)
                .Skip((resolvedPage - 1) * resolvedPageSize)
                .Take(resolvedPageSize)
                .ToListAsync();

            return Json(new
            {
                success = true,
                page = resolvedPage,
                pageSize = resolvedPageSize,
                totalItems,
                totalPages,
                customers = customers.Select(x => new
                {
                    x.UserId,
                    FullName = $"{x.LastName} {x.FirstName}".Trim(),
                    x.PhoneNumber,
                    x.Email,
                    AddressLine = x.Address?.AddressLine,
                    Ward = x.Address?.Ward,
                    District = x.Address?.District,
                    Province = x.Address?.Province,
                    Address = string.Join(", ", new[]
                    {
                        x.Address?.AddressLine,
                        x.Address?.Ward,
                        x.Address?.District,
                        x.Address?.Province
                    }.Where(part => !string.IsNullOrWhiteSpace(part)))
                })
            });
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
            var cus = await _context.Users.Include(x => x.Addresses).Select(x=> new {x.UserId,x.LastName,x.FirstName,x.PhoneNumber,x.Email,x.Addresses})
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
               
                var today = DateTime.Now;

                // Kiểm tra nếu voucher chưa bắt đầu
                if (voucher.StartedAt.HasValue && voucher.StartedAt.Value.Date > today)
                {
                    return Json(new { success = false, message = "Voucher chưa đến thời hạn sử dụng" });
                }

                // Kiểm tra nếu voucher đã hết hạn
                if (voucher.ExpiredAt.HasValue && voucher.ExpiredAt.Value.Date < today)
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
        [Route("GetAvailableVouchers")]
        public async Task<JsonResult> GetAvailableVouchers(string? keyword)
        {
            var today = DateTime.Now.Date;

            var query = _context.Vouchers
                .AsNoTracking()
                .Where(x =>
                    (!x.StartedAt.HasValue || x.StartedAt.Value.Date <= today) &&
                    (!x.ExpiredAt.HasValue || x.ExpiredAt.Value.Date >= today) &&
                    (x.Quantity ?? 0) > 0);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var normalizedKeyword = keyword.Trim();
                query = query.Where(x =>
                    (x.Code ?? string.Empty).Contains(normalizedKeyword) ||
                    (x.Name ?? string.Empty).Contains(normalizedKeyword));
            }

            var vouchers = await query
                .OrderBy(x => x.ExpiredAt ?? DateTime.MaxValue)
                .ThenByDescending(x => x.VoucherId)
                .Take(20)
                .Select(x => new
                {
                    x.VoucherId,
                    x.Code,
                    x.Name,
                    x.Promotion,
                    x.Quantity,
                    x.ExpiredAt
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                vouchers
            });
        }

        [HttpGet]
        [Route("GetProduct")]
        public async Task<IActionResult> GetProduct(int id)
        {
            var product = await _context.Products
                .AsNoTracking()
                .Where(x => x.ProductId == id
                         && x.Status != "outstock"
                         && x.Status != "discontinued")
                .Select(x => new
                {
                    x.ProductId,
                    x.Name,
                    SellPrice = x.SellPrice.HasValue
                        ? x.SellPrice.Value.ToString("C0", new CultureInfo("vi-VN"))
                        : null,
                    OriginalPrice = x.OriginalPrice.HasValue
                        ? x.OriginalPrice.Value.ToString("C0", new CultureInfo("vi-VN"))
                        : null,
                    x.Description,
                    x.Image,
                    x.Stock,
                    x.Sku,
                    Status = x.Status,

                    Category = new
                    {
                        x.Category.CategoryId,
                        x.Category.Name
                    },

                    Brand = new
                    {
                        x.Brand.BrandId,
                        x.Brand.Name
                    },

                    VarientProducts = x.VarientProducts
                        .Select(v => new
                        {
                            v.VarientId,
                            v.Sku,
                            v.Stock,
                            v.Price,
                            Values = v.VariantAttributes
                                .OrderBy(va => va.VariantAttributeId)
                                .Select(va => va.AttributeValue.Value)
                                .ToList()
                        })
                        .ToList()
                })
                .FirstOrDefaultAsync();

            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không hợp lệ" });
            }

            return Json(new { success = true, product });
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
                    x.Image,
                    x.Status
                })
               .Where(x => x.Status != "outstock" && x.Status != "discontinued")
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
                   x.Image,
                   x.Status
               })
               .Where(x => x.Status != "outstock" && x.Status != "discontinued")
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
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
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
            var variantValues = varient.VariantAttributes
                .OrderBy(va => va.VariantAttributeId)
                .Select(va => va.AttributeValue.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            var variantDisplay = variantValues.Count > 0
                ? string.Join(" / ", variantValues)
                : (string.IsNullOrWhiteSpace(varient.Attributes) ? varient.Sku : varient.Attributes);
            // Tạo HTML cho một dòng mới trong bảng
            var newRow = $@"
            <tr data-varient-id='{varient.VarientId}'>
                <td>
                    <div class='pos-cart-product'>
                        <span class='pos-cart-product__name'>{varient.Product.Name}</span>
                        <span class='pos-cart-product__sku'>{varient.Sku}</span>
                    </div>
                </td>
                <td><span class='pos-cart-variant'>{variantDisplay}</span></td>
                <td>
                    <span class='pos-cart-quantity'>{cart.Quantity}</span>
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
                .Include(v => v.VariantAttributes)
                    .ThenInclude(va => va.AttributeValue)
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
            var variantValues = varient.VariantAttributes
                .OrderBy(va => va.VariantAttributeId)
                .Select(va => va.AttributeValue.Value)
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .ToList();
            var variantDisplay = variantValues.Count > 0
                ? string.Join(" / ", variantValues)
                : (string.IsNullOrWhiteSpace(varient.Attributes) ? varient.Sku : varient.Attributes);

            // Tạo HTML cho dòng sản phẩm với số lượng cập nhật
            var newRow = $@"
            <tr data-varient-id='{varient.VarientId}'>
                <td>
                    <div class='pos-cart-product'>
                        <span class='pos-cart-product__name'>{varient.Product.Name}</span>
                        <span class='pos-cart-product__sku'>{varient.Sku}</span>
                    </div>
                </td>
                <td><span class='pos-cart-variant'>{variantDisplay}</span></td>
                <td>
                    <span class='pos-cart-quantity'>{cart.Quantity}</span>
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
                    decimal deductPrice = ParseCurrency(invoice.DeductPrice, invoice.TotalPrice);
                    decimal discountPrice = ParseCurrency(invoice.DiscountPrice, invoice.TotalPrice);
                    var order = new Order
                    {
                        UserId = invoice.UserId,
                        DeductAmount = deductPrice,
                        DiscountAmount = discountPrice,
                        OriginAmount = invoice.OriginTotalPrice,
                        TotalAmount = invoice.TotalPrice,
                        ShippingAddressId = address.AddressId,
                        OrderDate = DateTime.Now, // Ghi nhận thời gian tạo đơn hàng
                        OrderStatus = "Completed" // Trạng thái đơn hàng ban đầu
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
                            ProductId = (int)varientproduct.ProductId,
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
                  
                    var payment = new Payment
                    {
                        OrderId = order.OrderId,
                        Gateway = invoice.PaymentMethod,
                        AmountIn = invoice.TotalPrice,
                        TransactionDate = DateTime.Now,
                        PaymentStatus = "Paid", // Đánh dấu thanh toán thành công
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

                    await _notificationService.NotifyAsync(Events.NotificationTarget.Admins, "Đơn hàng hoàn tất", $"Đơn hàng {order.OrderId} đã được thanh toán hoàn tất ", "success", $"/Admin/Orders/View/{order.OrderId}");
                    await _notificationService.NotifyAsync(Events.NotificationTarget.SpecificUsers, "Đơn hàng hoàn tất", $"Đơn hàng {order.OrderId} đã được thanh toán hoàn tất ", "success", $"/user/MyOrders/OrderDetail/{order.OrderId}",new List<int> { invoice.UserId});

                    try
                    {
                        await SendInvoiceEmailAsync(order.OrderId);
                        return Json(new { success = true, message = "Đặt hàng thành công và đã gửi email hóa đơn cho khách hàng", id = order.OrderId });
                    }
                    catch
                    {
                        return Json(new { success = true, message = "Đặt hàng thành công nhưng chưa gửi được email hóa đơn cho khách hàng", id = order.OrderId });
                    }
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
            htmlTemplate = htmlTemplate.Replace("{{phoneNumber}}", invoice.User.PhoneNumber);
            htmlTemplate = htmlTemplate.Replace("{{orderID}}", invoice.OrderId.ToString());
            htmlTemplate = htmlTemplate.Replace("{{orderDate}}", invoice.OrderDate?.ToString("dd/MM/yyyy"));
            htmlTemplate = htmlTemplate.Replace("{{originAmount}}", invoice.OriginAmount?.ToString("C0", new CultureInfo("vi-VN")));
            htmlTemplate = htmlTemplate.Replace("{{discountAmount}}", invoice.DiscountAmount?.ToString("C0", new CultureInfo("vi-VN")));
            htmlTemplate = htmlTemplate.Replace("{{deductAmount}}", invoice.DeductAmount?.ToString("C0", new CultureInfo("vi-VN")));
            htmlTemplate = htmlTemplate.Replace("{{amount}}", payment.AmountIn.ToString("C0", new CultureInfo("vi-VN")));

            // Thay thế chi tiết sản phẩm
            var productRows = new StringBuilder();
            int index = 1;
            foreach (var item in invoice.OrderItems)
            {
                var variantDisplay = string.IsNullOrWhiteSpace(item.VarientProduct.Attributes)
                    ? string.Empty
                    : $" ({item.VarientProduct.Attributes})";
                var unitPrice = item.Price.ToString("C0", new CultureInfo("vi-VN"));
                var lineTotal = (item.Price * item.Quantity).ToString("C0", new CultureInfo("vi-VN"));

                productRows.Append($@"
                    <tr>
                        <td class='text-center'>{index++}</td>
                        <td>{item.Product.Name}{variantDisplay}</td>
                        <td class='text-center'>{item.Quantity}</td>
                        <td class='text-end'>{unitPrice}</td>
                        <td class='text-end'>{lineTotal}</td>
                    </tr>");
            }
            htmlTemplate = htmlTemplate.Replace("{{productRows}}", productRows.ToString());

            // Tạo PDF từ HTML
            var converter = new SelectPdf.HtmlToPdf();
            converter.Options.PdfPageSize = PdfPageSize.A4;
            converter.Options.PdfPageOrientation = PdfPageOrientation.Portrait;
            converter.Options.MarginTop = 18;
            converter.Options.MarginBottom = 18;
            converter.Options.MarginLeft = 16;
            converter.Options.MarginRight = 16;
            var doc = converter.ConvertHtmlString(htmlTemplate);

            // Trả về PDF để tải xuống
            byte[] pdf = doc.Save();
            doc.Close();

            return File(pdf, "application/pdf", $"invoice_{id}.pdf");

        }

        private async Task SendInvoiceEmailAsync(int orderId)
        {
            var order = await _context.Orders
                .AsNoTracking()
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(x => x.OrderItems)
                    .ThenInclude(oi => oi.VarientProduct)
                        .ThenInclude(vp => vp.VariantAttributes)
                            .ThenInclude(va => va.AttributeValue)
                .Include(x => x.Payments)
                .Include(x => x.ShippingAddress)
                .Include(x => x.User)
                .FirstOrDefaultAsync(x => x.OrderId == orderId);

            if (order == null || string.IsNullOrWhiteSpace(order.User.Email))
            {
                return;
            }

            var settings = await _context.Settings.AsNoTracking().ToListAsync();
            var baseUrl = $"{Request.Scheme}://{Request.Host}";
            var payment = order.Payments
                .OrderByDescending(x => x.TransactionDate ?? x.CreatedAt)
                .FirstOrDefault();

            var email = new InvoiceEmail
            {
                ToEmail = order.User.Email,
                CustomerName = $"{order.User.LastName} {order.User.FirstName}".Trim(),
                CustomerAddress = await BuildFullAddressAsync(order.ShippingAddress),
                CustomerPhone = order.User.PhoneNumber ?? string.Empty,
                InvoiceNumber = order.OrderId.ToString(),
                OrderDate = order.OrderDate,
                Products = order.OrderItems.Select(item => new ProductItem
                {
                    Name = BuildEmailProductName(item),
                    Quantity = item.Quantity,
                    Price = item.Price
                }).ToList(),
                ShippingFee = order.ShippingAmount ?? 0,
                ShippingAmount = order.ShippingAmount ?? 0,
                PaymentMethod = PaymentDisplayHelper.GetLabel(payment?.Gateway),
                IsPaid = string.Equals(payment?.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase),
                CompanyName = settings.FirstOrDefault(x => x.Key == "NameCompany")?.Value ?? settings.FirstOrDefault(x => x.Key == "NameWebsite")?.Value ?? "Tech Store",
                CompanyAddress = settings.FirstOrDefault(x => x.Key == "Address")?.Value ?? string.Empty,
                CompanyEmail = settings.FirstOrDefault(x => x.Key == "Email")?.Value ?? string.Empty,
                CompanyPhone = settings.FirstOrDefault(x => x.Key == "PhoneNumber")?.Value ?? string.Empty,
                CompanyWebsite = baseUrl,
                SupportEmail = settings.FirstOrDefault(x => x.Key == "Email")?.Value ?? string.Empty,
                SupportPhone = settings.FirstOrDefault(x => x.Key == "PhoneNumber")?.Value ?? string.Empty,
                InvoicePdfUrl = $"{baseUrl}/Admin/POS/Print-Invoice?id={order.OrderId}",
                LogoPath = GetAbsoluteLogoUrl(baseUrl, settings.FirstOrDefault(x => x.Key == "LogoUrl")?.Value),
                Subject = $"Cảm ơn quý khách - Hóa đơn #{order.OrderId}",
                OriginAmount = order.OriginAmount ?? order.OrderItems.Sum(x => x.Price * x.Quantity),
                DiscountAmount = order.DiscountAmount ?? 0,
                DeductAmount = order.DeductAmount ?? 0,
                TotalAmount = order.TotalAmount
            };

            await _emailService.SendEmailOrderCompleted(email);
        }

        private async Task<string> BuildFullAddressAsync(Address? shippingAddress)
        {
            if (shippingAddress == null)
            {
                return "-";
            }

            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(shippingAddress.AddressLine))
            {
                parts.Add(shippingAddress.AddressLine.Trim());
            }

            if (string.IsNullOrWhiteSpace(shippingAddress.Province) ||
                string.IsNullOrWhiteSpace(shippingAddress.District) ||
                string.IsNullOrWhiteSpace(shippingAddress.Ward))
            {
                return parts.Count > 0 ? string.Join(", ", parts) : "-";
            }

            var jsonString = await System.IO.File.ReadAllTextAsync("wwwroot/Province_VN.json");
            var provinces = JsonConvert.DeserializeObject<List<Province>>(jsonString);

            var province = provinces?.FirstOrDefault(p => p.Code == int.Parse(shippingAddress.Province));
            var district = province?.Districts?.FirstOrDefault(d => d.Code == int.Parse(shippingAddress.District));
            var ward = district?.Wards?.FirstOrDefault(w => w.Code == int.Parse(shippingAddress.Ward));

            if (!string.IsNullOrWhiteSpace(ward?.Name))
            {
                parts.Add(ward.Name);
            }

            if (!string.IsNullOrWhiteSpace(district?.Name))
            {
                parts.Add(district.Name);
            }

            if (!string.IsNullOrWhiteSpace(province?.Name))
            {
                parts.Add(province.Name);
            }

            return parts.Count > 0 ? string.Join(", ", parts) : "-";
        }

        private static string BuildEmailProductName(OrderItem orderItem)
        {
            var variantDisplay = BuildVariantDisplay(orderItem.VarientProduct);
            return variantDisplay == "-"
                ? orderItem.Product.Name
                : $"{orderItem.Product.Name} ({variantDisplay})";
        }

        private static string BuildVariantDisplay(VarientProduct variant)
        {
            var values = variant.VariantAttributes
                .OrderBy(x => x.VariantAttributeId)
                .Select(x => x.AttributeValue.Value)
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();

            if (values.Count > 0)
            {
                return string.Join(" / ", values);
            }

            return string.IsNullOrWhiteSpace(variant.Attributes) ? "-" : variant.Attributes;
        }

        private static string GetAbsoluteLogoUrl(string baseUrl, string? logoPath)
        {
            if (string.IsNullOrWhiteSpace(logoPath))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(logoPath, UriKind.Absolute, out _))
            {
                return logoPath;
            }

            return $"{baseUrl}/Upload/Logo/{logoPath.TrimStart('/')}";
        }

    }
}
