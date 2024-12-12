namespace Tech_Store.Models.ViewModel
{
    public class CommentVM
    {
        public int CommentId { get; set; }
        public int ProductId { get; set; }
        public int UserId { get; set; }
        public string Content { get; set; } = null!;
        public int? ParentCommentId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool? Status { get; set; }

        // Thông tin người dùng 
        public string? UserName { get; set; }
        public string? UserAvatar { get; set; }

        // Danh sách bình luận con
        public List<CommentVM> Replies { get; set; } = new List<CommentVM>();
    }
}
