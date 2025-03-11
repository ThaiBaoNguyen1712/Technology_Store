namespace Tech_Store.Helpers
{
    public class InvoiceEmail
    {
        public string ToEmail { get; set; }
        public string CustomerName { get; set; }
        public string CustomerAddress { get; set; }
        public string CustomerPhone { get; set; }
        public string InvoiceNumber { get; set; }
        public List<ProductItem> Products { get; set; }
        public decimal ShippingFee { get; set; }
        public string PaymentMethod { get; set; }
        public bool IsPaid { get; set; }
        public string CompanyName { get; set; }
        public string CompanyAddress { get; set; }
        public string CompanyEmail { get; set; }
        public string CompanyPhone { get; set; }
        public string CompanyWebsite { get; set; }
        public string SupportEmail { get; set; }
        public string SupportPhone { get; set; }
        public string InvoicePdfUrl { get; set; }
        public string LogoPath { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
    }

    public class ProductItem
    {
        public string Name { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
