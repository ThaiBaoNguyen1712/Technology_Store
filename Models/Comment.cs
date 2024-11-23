using System;
using System.Collections.Generic;

namespace Tech_Store.Models;

public partial class Comment
{
    public int CommentId { get; set; }

    public int ProductId { get; set; }

    public int UserId { get; set; }

    public string Content { get; set; } = null!;

    public int? ParentCommentId { get; set; }

    public DateTime? CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public bool? Status { get; set; }

    public virtual Product Product { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
