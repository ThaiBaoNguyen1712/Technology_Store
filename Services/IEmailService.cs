using Tech_Store.Helpers;

namespace Tech_Store.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(MailRequest mailrequest);
        Task SendEmailOrderCompleted(InvoiceEmail invoiceEmail);
    }
}
