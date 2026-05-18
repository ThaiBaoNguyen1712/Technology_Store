namespace Tech_Store.Models.Enums
{
    public enum OrderStatusType
    {
        Pending = 1,
        Confirmed = 2,
        Cancelled = 3
    }

    public static class OrderStatusTypeExtensions
    {
        public static string ToRecordValue(this OrderStatusType orderStatusType)
        {
            return orderStatusType switch
            {
                OrderStatusType.Pending => "Pending",
                OrderStatusType.Confirmed => "Confirmed",
                OrderStatusType.Cancelled => "Cancelled",
                _ => "Pending"
            };
        }
    }
}
