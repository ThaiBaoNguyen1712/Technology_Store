using MailKit.Search;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Tech_Store.Models;
using Tech_Store.Models.DTO;

namespace Tech_Store.Areas.Admin.Controllers
{
    [Authorize(Roles = "Admin")]
    [Area("Admin")]
    [Route("Admin/[controller]")]
    public class AttributesController : BaseAdminController
    {
        public AttributesController(ApplicationDbContext context) : base(context) { }


        [Route("GetAll")]
        public JsonResult GetAll()
        {
            var attributesList = _context.Attributes
                .Include(p => p.AttributeValues)
                .Select(att => new AttributeDTo
                {
                    AttributeId = att.AttributeId,
                    Name = att.Name,
                    Code = att.Code,
                    Value = att.AttributeValues != null
                        ? string.Join(", ", att.AttributeValues.Select(v => v.Value))
                        : string.Empty,
                    SortOrder = att.SortOrder
                }).OrderBy(x=>x.SortOrder).ToList();

            return Json(new
            {
                success = true,
                data = attributesList
            });
        }

        [HttpPost("Create")]
        public JsonResult Create(AttributeDTo attributeDto)
        {
            if(!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v=>v.Errors)
                    .Select(e=>e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = "Lỗi dữ liệu",errors });
            }

            // Sử dụng transaction để đảm bảo tính toàn vẹn của các thao tác
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Tạo đối tượng Attribute mới
                    var attribute = new Models.Attribute
                    {
                        Code = attributeDto.Code,
                        Name = attributeDto.Name,
                        SortOrder = attributeDto.SortOrder,
                        IsActive = true
                    };
                    _context.Attributes.Add(attribute);
                    _context.SaveChanges();

                    // Cắt chuỗi giá trị thành mảng
                    var value_Array = SliceValueAttributeString(attributeDto.Value);

                    // Thêm các giá trị vào bảng AttributeValues
                    foreach (var value in value_Array)
                    {
                        var attrValue = new AttributeValue
                        {
                            AttributeId = attribute.AttributeId,
                            Value = value
                        };
                        _context.AttributeValues.Add(attrValue);
                    }
                    _context.SaveChanges();

                    // Commit khi mọi thao tác thành công
                    transaction.Commit();

                    return Json(new { success = true, message = "Đã thêm thuộc tính thành công" });
                }
                catch (Exception ex)
                {
                    // Rollback nếu có lỗi xảy ra
                    transaction.Rollback();

                    // Log lỗi (có thể ghi vào log để theo dõi sau này)
                    // _logger.LogError(ex, "Lỗi khi thêm thuộc tính");

                    return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
                }
            }
        }
        [HttpPut("Update")]
        public JsonResult Update(AttributeDTo attributeDto)
        {
            // Dùng transaction để đảm bảo toàn vẹn
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Lấy attribute cũ
                    var attribute = _context.Attributes.FirstOrDefault(a => a.AttributeId == attributeDto.AttributeId);
                    if (attribute == null)
                    {
                        return Json(new { success = false, message = "Thuộc tính không tồn tại" });
                    }

                    // Cập nhật thông tin thuộc tính
                    attribute.Code = attributeDto.Code;
                    attribute.Name = attributeDto.Name;
                    attribute.SortOrder = attributeDto.SortOrder;
                    attribute.IsActive = attributeDto.IsActive;
                    _context.Attributes.Update(attribute);
                    _context.SaveChanges();

                    // Lấy danh sách attribute values cũ
                    var oldValues = _context.AttributeValues
                        .Where(av => av.AttributeId == attribute.AttributeId)
                        .ToList();

                    // Xóa chỉ những giá trị chưa được sử dụng (giả sử bảng VariantAttributes chứa FK đến AttributeValues)
                    foreach (var oldValue in oldValues)
                    {
                        bool isUsed = _context.VariantAttributes.Any(va => va.AttributeValueId == oldValue.AttributeValueId);
                        if (!isUsed)
                        {
                            _context.AttributeValues.Remove(oldValue);
                        }
                    }
                    _context.SaveChanges();

                    // Cắt chuỗi giá trị thành mảng (tùy theo logic của hàm SliceValueAttributeString)
                    var newValuesArray = SliceValueAttributeString(attributeDto.Value);

                    // Thêm các giá trị mới (chỉ thêm nếu chưa tồn tại)
                    foreach (var newValue in newValuesArray)
                    {
                        // Kiểm tra nếu đã tồn tại, so sánh không phân biệt chữ hoa thường
                        bool exists = _context.AttributeValues
                            .Any(av => av.AttributeId == attribute.AttributeId &&
                                       av.Value.ToLower() == newValue.ToLower());
                        if (!exists)
                        {
                            var attrValue = new AttributeValue
                            {
                                AttributeId = attribute.AttributeId,
                                Value = newValue
                            };
                            _context.AttributeValues.Add(attrValue);
                        }
                    }
                    _context.SaveChanges();

                    // Commit giao dịch nếu mọi thứ đều ổn
                    transaction.Commit();

                    return Json(new { success = true, message = "Đã sửa thuộc tính thành công" });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
                }
            }
        }



        [HttpPost("Delete")]
        public JsonResult Delete(int attributeId)
        {
            // Sử dụng transaction để đảm bảo tính toàn vẹn của các thao tác
            using (var transaction = _context.Database.BeginTransaction())
            {
                try
                {
                    // Lấy thuộc tính và các giá trị liên quan
                    var attribute = _context.Attributes.FirstOrDefault(a => a.AttributeId == attributeId);
                    if (attribute == null)
                    {
                        return Json(new { success = false, message = "Thuộc tính không tồn tại" });
                    }

                    var attributeValues = _context.AttributeValues.Where(av => av.AttributeId == attributeId).ToList();

                    // Xóa các giá trị thuộc tính
                    _context.AttributeValues.RemoveRange(attributeValues);
                    _context.SaveChanges();

                    // Xóa thuộc tính
                    _context.Attributes.Remove(attribute);
                    _context.SaveChanges();

                    // Commit giao dịch khi mọi thao tác thành công
                    transaction.Commit();

                    return Json(new { success = true, message = "Đã xóa thuộc tính thành công" });
                }
                catch (Exception ex)
                {
                    // Rollback giao dịch nếu có lỗi xảy ra
                    transaction.Rollback();

                    return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
                }
            }
        }

        // Cắt chuỗi value string, khi người dùng điền vào thường có chuỗi "Xanh,đỏ,tím,vàng"
        private string[] SliceValueAttributeString(string valueString)
        {
            // Kiểm tra nếu chuỗi rỗng hoặc null
            if (string.IsNullOrWhiteSpace(valueString))
            {
                return new string[] { }; // Trả về mảng rỗng nếu chuỗi nhập vào trống
            }

            // Cắt chuỗi dựa trên dấu phẩy
            string[] values = valueString.Split(',');

            // Loại bỏ khoảng trắng ở đầu hoặc cuối từng phần tử
            values = values.Select(v => v.Trim()).ToArray();

            return values; // Trả về mảng các giá trị đã cắt
        }



    }
}
