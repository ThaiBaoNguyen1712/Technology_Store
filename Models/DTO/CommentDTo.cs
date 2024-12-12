namespace Tech_Store.Models.DTO
{
    public class CommentDTo
    {
        public int? Id { get; set; } 
        public int ProductId { get; set; }
        public string Content { get; set; }
        public int? ParentCommentId{ get; set; }
    }
}
