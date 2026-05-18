namespace Tech_Store.Models.DTO;

public class OrderReviewSubmitDto
{
    public int OrderId { get; set; }

    public List<OrderReviewItemSubmitDto> Items { get; set; } = [];
}

public class OrderReviewItemSubmitDto
{
    public int VariantId { get; set; }

    public int StarPoint { get; set; }

    public string Content { get; set; } = string.Empty;
}
