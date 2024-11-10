namespace Tech_Store.Models.DTO.Payment.Client.Momo
{
    public class MomoPaymentResponseModel
    {
        public string RequestId { get; set; }
        public int ErrorCode { get; set; }
        public string OrderId { get; set; }
        public string Message { get; set; }
        public string LocalMessage { get; set; }
        public string RequestType { get; set; }
        public string PayUrl { get; set; }
        public string Signature { get; set; }
        public string QrCodeUrl {  get; set; }
        public string Deeplink { get; set; }
        public string DeeplinkWebInApp { get; set; }
    }
    public class MomoPaymentResquestModel
    {
        public int OrderId { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public double Amount { get; set; }
        public DateTime CreatedDate { get; set; }
    }
}
