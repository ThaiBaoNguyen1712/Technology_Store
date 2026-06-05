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
                    IsActive = att.IsActive,
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

        [HttpGet("GetNextMetadata")]
        public JsonResult GetNextMetadata()
        {
            var nextSortOrder = (_context.Attributes.Max(x => (int?)x.SortOrder) ?? 0) + 1;
            var nextCodeNumber = (_context.Attributes.Count() + 1).ToString("D3");

            return Json(new
            {
                success = true,
                code = $"attr_{nextCodeNumber}",
                sortOrder = nextSortOrder
            });
        }

        [HttpPost("Create")]
        public async Task<JsonResult> Create(AttributeDTo attributeDto)
        {
            if(!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v=>v.Errors)
                    .Select(e=>e.ErrorMessage)
                    .ToList();

                return Json(new { success = false, message = "Lỗi dữ liệu",errors });
            }

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var attribute = new Models.Attribute
                {
                    Code = attributeDto.Code,
                    Name = attributeDto.Name,
                    SortOrder = attributeDto.SortOrder,
                    IsActive = attributeDto.IsActive
                };
                _context.Attributes.Add(attribute);
                await _context.SaveChangesAsync();

                var value_Array = SliceValueAttributeString(attributeDto.Value);

                foreach (var value in value_Array)
                {
                    var attrValue = new AttributeValue
                    {
                        AttributeId = attribute.AttributeId,
                        Value = value
                    };
                    _context.AttributeValues.Add(attrValue);
                }
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã thêm thuộc tính thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }
        [HttpPut("Update")]
        public async Task<JsonResult> Update(AttributeDTo attributeDto)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var attribute = await _context.Attributes.FirstOrDefaultAsync(a => a.AttributeId == attributeDto.AttributeId);
                if (attribute == null)
                {
                    return Json(new { success = false, message = "Thuộc tính không tồn tại" });
                }

                attribute.Code = attributeDto.Code;
                attribute.Name = attributeDto.Name;
                attribute.SortOrder = attributeDto.SortOrder;
                attribute.IsActive = attributeDto.IsActive;
                _context.Attributes.Update(attribute);
                await _context.SaveChangesAsync();

                var oldValues = await _context.AttributeValues
                    .Where(av => av.AttributeId == attribute.AttributeId)
                    .ToListAsync();

                foreach (var oldValue in oldValues)
                {
                    bool isUsed = await _context.VariantAttributes.AnyAsync(va => va.AttributeValueId == oldValue.AttributeValueId);
                    if (!isUsed)
                    {
                        _context.AttributeValues.Remove(oldValue);
                    }
                }
                await _context.SaveChangesAsync();

                var newValuesArray = SliceValueAttributeString(attributeDto.Value);

                foreach (var newValue in newValuesArray)
                {
                    bool exists = await _context.AttributeValues
                        .AnyAsync(av => av.AttributeId == attribute.AttributeId &&
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
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã sửa thuộc tính thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }



        [HttpPost("Delete")]
        public async Task<JsonResult> Delete(int attributeId)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var attribute = await _context.Attributes.FirstOrDefaultAsync(a => a.AttributeId == attributeId);
                if (attribute == null)
                {
                    return Json(new { success = false, message = "Thuộc tính không tồn tại" });
                }

                var attributeValues = await _context.AttributeValues.Where(av => av.AttributeId == attributeId).ToListAsync();

                _context.AttributeValues.RemoveRange(attributeValues);
                await _context.SaveChangesAsync();

                _context.Attributes.Remove(attribute);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();

                return Json(new { success = true, message = "Đã xóa thuộc tính thành công" });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
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
