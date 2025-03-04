using System.ComponentModel.DataAnnotations;

namespace Tech_Store.Models.DTO
{
    public class AttributeDTo
    {
        public int? AttributeId { get; set; }

        [Required(ErrorMessage = "Yêu cầu thêm tên Thuộc tính")]
        [StringLength(255)]
        public string Name { get; set; }

        [Required(ErrorMessage = "Yêu cầu thêm mã thuộc tính")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Yêu cầu trạng thái kích hoạt")]
        public bool IsActive { get; set; }

        [Required(ErrorMessage = "Yêu cầu sắp xếp thứ tự")]
        public int SortOrder { get; set; }

        [Required(ErrorMessage = "Yêu cầu nhập giá trị")]
        public string Value { get; set; }
    }

}
