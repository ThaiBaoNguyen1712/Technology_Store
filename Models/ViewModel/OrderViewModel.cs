namespace Tech_Store.Models.ViewModel
{
    public class OrderViewModel
    {
        public int OrderId {  get; set; }
        public string NameCustomer { get; set; }
        public string PhoneNumber { get; set; }
        public decimal TotalAmount { get; set; }
        public string OrderStatus {  get; set; }
        public DateTime OrderDate { get; set; }
        public string PaymentStatus {  get; set; }
        public string ListProducts { get; set; }
    }
}
