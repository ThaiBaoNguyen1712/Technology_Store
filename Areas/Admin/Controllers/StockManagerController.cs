using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Tech_Store.Models;
using Tech_Store.Models.DTO;
using X.PagedList.Extensions;
using X.PagedList;
using OfficeOpenXml;
using System.Linq;
using OfficeOpenXml.Style;
using System.Drawing;
namespace Tech_Store.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class StockManagerController : BaseAdminController
    {
        public StockManagerController(ApplicationDbContext context) : base(context) { }
        [Route("")]
        [Route("index")]
        public IActionResult Index()
        {
            var list_products = _context.Products
               .Include(p => p.Brand).Include(p => p.Category)
               .OrderByDescending(x => x.ProductId).ToList().Take(100);
            var list_cate = _context.Categories.ToList();
            ViewBag.cate = list_cate;
            var list_brand = _context.Brands.ToList();
            ViewBag.brand = list_brand;
            return View(list_products);
        }

        [HttpPost]
        [Route("Filter")]
        public IActionResult Filter(string sku, string name, string status, int? categoryId, int? brandId, int? stockFrom, int? stockTo)
        {
            var products = _context.Products
                .Include(p => p.Brand)
                .Include(p => p.Category)
                .AsQueryable();

            // Áp dụng các tiêu chí lọc
            if (!string.IsNullOrEmpty(sku))
                products = products.Where(p => p.Sku.Contains(sku));
            if (!string.IsNullOrEmpty(name))
                products = products.Where(p => p.Name.Contains(name));
            if (!string.IsNullOrEmpty(status))
                products = products.Where(p => p.Status == status);
            if (categoryId.HasValue)
                products = products.Where(p => p.CategoryId == categoryId.Value);
            if (brandId.HasValue)
                products = products.Where(p => p.BrandId == brandId.Value);
            if (stockFrom.HasValue)
                products = products.Where(p => p.Stock >= stockFrom.Value);
            if (stockTo.HasValue)
                products = products.Where(p => p.Stock <= stockTo.Value);

            // Chuyển đổi sang view model để trả về JSON
            var result = products.OrderByDescending(p => p.ProductId)
                .Take(100)
                .Select(p => new ProductViewModel
                {
                    ProductId = p.ProductId,
                    Image = p.Image,
                    Name = p.Name,
                    Sku = p.Sku,
                    BrandName = p.Brand.Name,
                    CategoryName = p.Category.Name,
                    SellPrice = p.SellPrice,
                    Stock = p.Stock,
                    Status = p.Status
                })
                .ToList();

            return Json(result);
        }
        [HttpGet("GetProduct/{id}")]
        public async Task<JsonResult> GetProduct(int id)
        {
            var product = await _context.Products.Include(p => p.VarientProducts).FirstOrDefaultAsync(x => x.ProductId == id);
            if (product == null)
            {
                return Json(new { success = false, message = "Không tìm thấy" });
            }
            return Json(new {success=true,product});
        }

        [HttpPost("AddProductHistory")]
        public async Task<JsonResult> AddProductHistory([FromBody] ProductHistoryDTo productHistoryDTo)
        {
            if (!ModelState.IsValid)
            {
                return Json(new { success = false, message = "Dữ liệu đầu vào không hợp lệ" });
            }

            using (var transaction = await _context.Database.BeginTransactionAsync())
            {
                try
                {
                    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                    var productHistory = new InventoryTransactions
                    {
                        ProductId = productHistoryDTo.ProductId,
                        Type = productHistoryDTo.Type,
                        UserId = int.Parse(userId),
                        Note = productHistoryDTo.Note,
                        CreatedAt = DateTime.Now
                    };

                    await _context.InventoryTransactions.AddAsync(productHistory);
                    await _context.SaveChangesAsync();

                    var product = await _context.Products
                        .FirstOrDefaultAsync(x => x.ProductId == productHistoryDTo.ProductId);

                    if (product == null)
                    {
                        return Json(new { success = false, message = "Sản phẩm không tồn tại" });
                    }

                    List<InventoryTransactionsDetail> productHistoryDetails = new List<InventoryTransactionsDetail>();

                    foreach (var item in productHistoryDTo.Variants)
                    {
                        var variant = await _context.VarientProducts
                            .FirstOrDefaultAsync(x => x.VarientId == item.VariantId);

                        if (variant == null)
                        {
                            return Json(new { success = false, message = $"Biến thể sản phẩm với ID {item.VariantId} không tồn tại" });
                        }

                        int quantityChange = item.Quantity;

                        if (productHistoryDTo.Type == "Import")
                        {
                            product.Stock += quantityChange;
                            variant.Stock += quantityChange;
                        }
                        else
                        {
                            product.Stock -= quantityChange;
                            variant.Stock -= quantityChange;

                            if (product.Stock < 0 || variant.Stock < 0)
                            {
                                return Json(new { success = false, message = "Số lượng tồn kho không đủ để thực hiện giao dịch" });
                            }
                        }

                        var productHistoryDetail = new InventoryTransactionsDetail
                        {
                            InventoryTransId = productHistory.InventoryTransId,
                            VarientId = item.VariantId,
                            Quantity = quantityChange
                        };
                        productHistoryDetails.Add(productHistoryDetail);
                    }

                    if (product.Stock == 0)
                    {
                        product.Status = "outstock";
                    }
                    else if (product.Status.Contains("outstock") && product.Stock > 0)
                    {
                        product.Status = "available";
                    }

                    await _context.InventoryTransactionsDetail.AddRangeAsync(productHistoryDetails);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return Json(new { success = true, message = "Thêm thành công" });
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
                }
            }
        }

        [Route("History")]
        public IActionResult History(int? page)
        {
            int pageSize = 5; // Số lượng bản ghi trên mỗi trang
            int pageNumber = page ?? 1; // Trang hiện tại, mặc định là 1

            var history = _context.InventoryTransactions
                .Include(p => p.Product)
                .Include(p => p.InventoryTransactionsDetail) // Bao gồm chi tiết lịch sử sản phẩm
                .Include(p => p.User)
                .ThenInclude(p => p.Roles)
                .Select(ph => new InventoryTransactionsVM
                {
                    Id = ph.InventoryTransId,
                    Product = ph.Product,
                    InventoryTransId = ph.InventoryTransId,
                    Type = ph.Type,
                    Note = ph.Note,
                    UserName = ph.User.LastName + " " + ph.User.FirstName,
                    UserRole = ph.User.Roles.FirstOrDefault().RoleName,
                    InventoryTransactionDetail = ph.InventoryTransactionsDetail
                        .Select(d => new InventorTransactionDetailViewModel // Ánh xạ từng ProductHistoryDetail thành ProductHistoryDetailViewModel
                        {
                            Quantity = d.Quantity
                        })
                        .ToList() // Chuyển đổi thành danh sách
                })
                .OrderByDescending(ph => ph.Id) // Sắp xếp giảm dần theo ID
                .ToPagedList(pageNumber, pageSize); // Áp dụng phân trang

            return View(history);
        }


        [HttpGet("GetHistoryDetail/{id}")]
        public async Task<JsonResult> GetHistoryDetail(int id)
        {
            var history = await _context.InventoryTransactions
                .Where(ph => ph.InventoryTransId == id) // Điều kiện lọc trước khi Select
                .Include(p => p.Product)
                .Include(p => p.User)
                .ThenInclude(p => p.Roles)
                .Include(p => p.InventoryTransactionsDetail).ThenInclude(p => p.Varient)
                .Select(ph => new InventoryTransactionsVM
                {
                    Id = ph.InventoryTransId,
                    Product = ph.Product,
                    Type = ph.Type,
                    Note = ph.Note,
                    CreatedAt = (DateTime)ph.CreatedAt,
                    InventoryTransactionDetail = ph.InventoryTransactionsDetail.Select(d => new InventorTransactionDetailViewModel
                    {
                        InventoryTransId = d.InventoryTransId,
                        VarientId = d.VarientId,
                        VarientSku = d.Varient.Sku,
                        Quantity = d.Quantity,
                        VarientName = d.Varient.Attributes // Optional, if needed
                    }).ToList(), // No need to cast here
                    UserName = ph.User.LastName + " " + ph.User.FirstName,
                    UserRole = ph.User.Roles.FirstOrDefault().RoleName ?? "Không có vai trò" // Tránh lỗi null
                })
                .FirstOrDefaultAsync();

            if (history == null)
            {
                return Json(new { success = false, message = "Không tìm thấy chi tiết lịch sử" });
            }

            return Json(new { success = true, history });
        }
        [HttpPost("FilterHistoryDetail")]
        public IActionResult FilterHistoryDetail(DateOnly? startDate, DateOnly? endDate, string? filterType, string? filterCode, int page = 1)
        {
            int pageSize = 5; // Số lượng bản ghi mỗi trang

            // Truy vấn cơ bản từ ProductHistories
            var query = _context.InventoryTransactions
                .Include(ph => ph.InventoryTransactionsDetail)
                .Include(ph => ph.Product)
                .Include(ph => ph.User)
                .ThenInclude(u => u.Roles)
                .AsQueryable();

            // Áp dụng bộ lọc
            if (startDate.HasValue)
            {
                query = query.Where(ph => ph.CreatedAt >= startDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            if (endDate.HasValue)
            {
                query = query.Where(ph => ph.CreatedAt <= endDate.Value.ToDateTime(TimeOnly.MinValue));
            }

            if (!string.IsNullOrEmpty(filterType))
            {
                query = query.Where(ph => ph.Type.ToLower() == filterType.ToLower());
            }

            if (!string.IsNullOrEmpty(filterCode))
            {
                query = query.Where(ph => ph.Product.Sku.Contains(filterCode) || ph.InventoryTransId.ToString().Contains(filterCode));
            }

            // Áp dụng phân trang và ánh xạ sang ProductHistoryViewModel
            var result = query
                .OrderByDescending(ph => ph.InventoryTransId) // Sắp xếp giảm dần theo ID
                .ToPagedList(page, pageSize)
                .Select(ph => new InventoryTransactionsVM
                {
                    Id = ph.InventoryTransId,
                    Product = ph.Product,
                    InventoryTransId = ph.InventoryTransId,
                    Type = ph.Type,
                    Note = ph.Note,
                    UserName = ph.User.LastName + " " + ph.User.FirstName,
                    UserRole = ph.User.Roles.FirstOrDefault()?.RoleName,
                    InventoryTransactionDetail = ph.InventoryTransactionsDetail  
                        .Select(d => new InventorTransactionDetailViewModel
                        {
                            Quantity = d.Quantity
                        })
                        .ToList()
                });
            ViewBag.FilterCode = filterCode;
            ViewBag.FilterType = filterType; 
            ViewBag.StartDate = startDate;
            ViewBag.EndDate = endDate;
            // Trả về view "History" với kết quả phân trang
            return View("History", result);
        }

        [HttpGet("GetVariantProduct")]
        public async Task<JsonResult> GetVariantProduct()
        {
            // Truy vấn dữ liệu từ bảng VariantProducts và bao gồm liên kết Product
            var variants = await _context.VarientProducts
                .Include(x => x.Product)
                .ThenInclude(p => p.Category).OrderByDescending(x=>x.VarientId) // Nếu bạn có bảng Category liên quan
                .ToListAsync();

            var variant_return = variants.Select(variant => new VariantViewModel

            {

                Id = variant.VarientId,

                ProductId = variant.ProductId ?? 0, 

                ProductName = variant.Product?.Name ?? "N/A", 

                Sku = variant.Sku ?? "N/A", 

                Attribute = variant.Attributes ?? "N/A", 

                SellPrice = variant.Price ?? 0M, 

                Stock = variant.Stock ?? 0,

                ImageUrl = variant.Product.Image ?? "none.png",

                CategoryName = variant.Product?.Category?.Name ?? "N/A" 

            }).ToList();


            return Json(variant_return);
        }
        [HttpPost("ExportToExcel")]
        public IActionResult ExportToExcel([FromBody] string[] VariantIds)
        {
            // Lấy danh sách biến thể từ database
            var variants = _context.VarientProducts
                .Include(x => x.Product)
                .ThenInclude(p => p.Category)
                .Where(x => VariantIds.Contains(x.VarientId.ToString()))
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Products");

                // Định dạng tiêu đề
                var titleRow = 1;
                var headerRow = 3;

                // Thêm tiêu đề chính
                worksheet.Cells[titleRow, 1].Value = "BÁO CÁO THÔNG TIN SẢN PHẨM";
                using (var range = worksheet.Cells[titleRow, 1, titleRow, 6])
                {
                    range.Merge = true;
                    range.Style.Font.Size = 16;
                    range.Style.Font.Bold = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 220, 220, 220));
                }

                // Thêm ngày xuất báo cáo
                worksheet.Cells[titleRow + 1, 1].Value = $"Ngày xuất báo cáo: {DateTime.Now.ToString("dd/MM/yyyy HH:mm")}";
                using (var range = worksheet.Cells[titleRow + 1, 1, titleRow + 1, 6])
                {
                    range.Merge = true;
                    range.Style.Font.Italic = true;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                }

                // Định dạng header
                var headers = new string[] { "SKU", "TÊN SẢN PHẨM", "BIẾN THỂ", "DANH MỤC", "ĐƠN GIÁ", "SL TỒN" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[headerRow, i + 1].Value = headers[i];
                    using (var range = worksheet.Cells[headerRow, i + 1])
                    {
                        range.Style.Font.Bold = true;
                        range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                        range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 184, 204, 228));
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;
                        range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    }
                }

                // Điền dữ liệu và định dạng
                var dataStartRow = headerRow + 1;
                for (int i = 0; i < variants.Count; i++)
                {
                    var currentRow = dataStartRow + i;
                    var variant = variants[i];

                    worksheet.Cells[currentRow, 1].Value = variant.Sku ?? "N/A";
                    worksheet.Cells[currentRow, 2].Value = variant.Product?.Name ?? "N/A";
                    worksheet.Cells[currentRow, 3].Value = variant.Attributes ?? "N/A";
                    worksheet.Cells[currentRow, 4].Value = variant.Product?.Category?.Name ?? "N/A";
                    worksheet.Cells[currentRow, 5].Value = variant.Price ?? 0;
                    worksheet.Cells[currentRow, 6].Value = variant.Stock ?? 0;

                    // Định dạng các ô dữ liệu
                    using (var range = worksheet.Cells[currentRow, 1, currentRow, 6])
                    {
                        range.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                        range.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                        // Thêm màu xen kẽ cho các dòng
                        if (i % 2 == 1)
                        {
                            range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                            range.Style.Fill.BackgroundColor.SetColor(Color.FromArgb(255, 242, 242, 242));
                        }
                    }

                    // Định dạng cột giá
                    worksheet.Cells[currentRow, 5].Style.Numberformat.Format = "#,##0";
                    worksheet.Cells[currentRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                    // Định dạng cột số lượng
                    worksheet.Cells[currentRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                }

                // Thêm footer với tổng kết
                var footerRow = dataStartRow + variants.Count + 1;
                worksheet.Cells[footerRow, 1].Value = "Tổng cộng:";
                worksheet.Cells[footerRow, 1, footerRow, 4].Merge = true;
                worksheet.Cells[footerRow, 1, footerRow, 4].Style.Font.Bold = true;
                worksheet.Cells[footerRow, 1, footerRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;

                // Tính tổng giá trị và số lượng
                worksheet.Cells[footerRow, 5].Formula = $"SUM(E{dataStartRow}:E{footerRow - 1})";
                worksheet.Cells[footerRow, 6].Formula = $"SUM(F{dataStartRow}:F{footerRow - 1})";
                worksheet.Cells[footerRow, 5].Style.Numberformat.Format = "#,##0";
                worksheet.Cells[footerRow, 5, footerRow, 6].Style.Font.Bold = true;
                worksheet.Cells[footerRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
                worksheet.Cells[footerRow, 6].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

                // Thêm ghi chú cuối trang
                var noteRow = footerRow + 2;
                worksheet.Cells[noteRow, 1].Value = "Ghi chú:";
                worksheet.Cells[noteRow, 2, noteRow, 6].Value = "Báo cáo được tạo tự động từ hệ thống";
                worksheet.Cells[noteRow, 1].Style.Font.Bold = true;
                worksheet.Cells[noteRow, 2, noteRow, 6].Style.Font.Italic = true;

                // Tự động điều chỉnh độ rộng cột
                worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();

                // Đặt độ rộng tối thiểu và tối đa cho các cột
                for (int i = 1; i <= 6; i++)
                {
                    double width = worksheet.Column(i).Width;
                    if (width < 10) worksheet.Column(i).Width = 10;
                    else if (width > 50) worksheet.Column(i).Width = 50;
                }

                // Thêm bảo vệ worksheet (tùy chọn)
                worksheet.Protection.IsProtected = true;
                worksheet.Protection.AllowSelectLockedCells = true;

                // Lưu file
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(
                    stream,
                    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                    $"Products_Report_{DateTime.Now:yyyyMMdd_HHmmss}.xlsx"
                );
            }
        }


    }
}
