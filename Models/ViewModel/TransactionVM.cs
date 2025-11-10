namespace Tech_Store.Models.ViewModel
{
    public class TransactionVM
    {
        public int TransId { get; set; }
        public int InvoiceId { get; set; }
        public string PaymentMethod { get; set; }
        public string CustomerName { get; set; }
         public string PhoneNumber { get; set; }
         public string Status { get; set; }
         public DateTime? PaymentDate { get; set; }
         public decimal Amount { get; set; }

    }
}
