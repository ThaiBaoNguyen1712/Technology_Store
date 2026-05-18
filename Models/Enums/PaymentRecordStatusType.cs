namespace Tech_Store.Models.Enums
{
    public enum PaymentRecordStatusType
    {
        Unpaid = 1,
        Paid = 2
    }

    public static class PaymentRecordStatusTypeExtensions
    {
        public static string ToRecordValue(this PaymentRecordStatusType paymentRecordStatusType)
        {
            return paymentRecordStatusType switch
            {
                PaymentRecordStatusType.Unpaid => "Unpaid",
                PaymentRecordStatusType.Paid => "Paid",
                _ => "Unpaid"
            };
        }
    }
}
