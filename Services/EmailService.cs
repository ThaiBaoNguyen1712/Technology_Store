using Tech_Store.Helpers;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using System.Text;
using System.Globalization;


namespace Tech_Store.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _emailSettings;

        public EmailService(IOptions<EmailSettings> options)
        {
            _emailSettings = options.Value;
        }

        public async Task SendEmailAsync(MailRequest mailRequest)
        {
            var email = new MimeMessage();
            email.Sender = MailboxAddress.Parse(_emailSettings.Email);
            email.To.Add(MailboxAddress.Parse(mailRequest.ToEmail));
            email.Subject = mailRequest.Subject;

            var builder = new BodyBuilder
            {
                HtmlBody = mailRequest.Body
            };
            email.Body = builder.ToMessageBody();

            using var smtp = new SmtpClient();
            smtp.Connect(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
            smtp.Authenticate(_emailSettings.Email, _emailSettings.Password);
            await smtp.SendAsync(email);
            smtp.Disconnect(true);
        }
        public async Task SendEmailOrderCompleted(InvoiceEmail invoiceEmail)
        {
            try
            {
                if (invoiceEmail == null)
                {
                    throw new ArgumentNullException(nameof(invoiceEmail), "Dữ liệu hóa đơn không được null.");
                }

                if (invoiceEmail.Products == null || invoiceEmail.Products.Count == 0)
                {
                    throw new InvalidOperationException("Danh sách sản phẩm không được rỗng.");
                }

                // Đọc template HTML từ file
                string templatePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Template", "ThanksAndInvoice.html");

                if (!File.Exists(templatePath))
                {
                    throw new FileNotFoundException("Không tìm thấy file template.", templatePath);
                }

                string htmlBody = await File.ReadAllTextAsync(templatePath);

                // Thay thế các placeholder trong template với dữ liệu thực tế
                htmlBody = htmlBody.Replace("{Tên khách hàng}", invoiceEmail.CustomerName ?? string.Empty)
                                   .Replace("{Số hóa đơn}", invoiceEmail.InvoiceNumber ?? string.Empty)
                                   .Replace("{Ngày lập hóa đơn}", (invoiceEmail.OrderDate ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm"))
                                   .Replace("{Địa chỉ}", invoiceEmail.CustomerAddress ?? string.Empty)
                                   .Replace("{Email}", invoiceEmail.ToEmail ?? string.Empty)
                                   .Replace("{Số điện thoại}", invoiceEmail.CustomerPhone ?? string.Empty);

                // Tạo bảng sản phẩm từ danh sách sản phẩm
                StringBuilder productRows = new StringBuilder();
                decimal productSubtotal = 0;

                for (int i = 0; i < invoiceEmail.Products.Count; i++)
                {
                    var product = invoiceEmail.Products[i];
                    decimal lineTotal = product.Price * product.Quantity;
                    productSubtotal += lineTotal;

                    productRows.AppendLine($@"<tr>
                <td>{i + 1}</td>
                <td>{product.Name}</td>
                <td>{product.Quantity}</td>
                <td>{product.Price.ToString("N0")} VNĐ</td>
                <td>{lineTotal.ToString("N0")} VNĐ</td>
            </tr>");
                }

                var originAmount = invoiceEmail.OriginAmount ?? productSubtotal;
                var discountAmount = invoiceEmail.DiscountAmount ?? 0;
                var deductAmount = invoiceEmail.DeductAmount ?? 0;
                var shippingFee = invoiceEmail.ShippingAmount ?? invoiceEmail.ShippingFee;
                var grandTotal = invoiceEmail.TotalAmount ?? Math.Max(0, originAmount - discountAmount - deductAmount + shippingFee);
                var discountRowHtml = discountAmount > 0
                    ? $@"<p>Giảm giá khuyến mãi: {discountAmount.ToString("N0", CultureInfo.InvariantCulture)} VNĐ</p>"
                    : string.Empty;
                var deductRowHtml = deductAmount > 0
                    ? $@"<p>Giảm giá thêm: {deductAmount.ToString("N0", CultureInfo.InvariantCulture)} VNĐ</p>"
                    : string.Empty;

                // Thay thế thông tin tổng tiền
                htmlBody = htmlBody.Replace("{Tổng tiền hàng}", originAmount.ToString("N0") + " VNĐ")
                                   .Replace("{Dòng giảm giá}", discountRowHtml)
                                   .Replace("{Dòng giảm thêm}", deductRowHtml)
                                   .Replace("{Phí vận chuyển}", shippingFee.ToString("N0") + " VNĐ")
                                   .Replace("{Tổng thanh toán}", grandTotal.ToString("N0") + " VNĐ")
                                   .Replace("{Phương thức thanh toán}", invoiceEmail.PaymentMethod ?? string.Empty)
                                   .Replace("{ProductRows}",productRows.ToString())
                                   .Replace("{Đã thanh toán/Chưa thanh toán}", invoiceEmail.IsPaid ? "Đã thanh toán" : "Chưa thanh toán");

                // Thay thế thông tin công ty
                htmlBody = htmlBody.Replace("{Tên công ty}", invoiceEmail.CompanyName ?? string.Empty)
                                    .Replace("{logoUrl}",invoiceEmail.LogoPath ?? string.Empty)
                                   .Replace("{Năm hiện tại}", DateTime.Now.Year.ToString())
                                   .Replace("{Địa chỉ công ty}", invoiceEmail.CompanyAddress ?? string.Empty)
                                   .Replace("{Email công ty}", invoiceEmail.CompanyEmail ?? string.Empty)
                                   .Replace("{Số điện thoại công ty}", invoiceEmail.CompanyPhone ?? string.Empty)
                                   .Replace("{Website công ty}", invoiceEmail.CompanyWebsite ?? string.Empty)
                                   .Replace("{email hỗ trợ}", invoiceEmail.SupportEmail ?? string.Empty)
                                   .Replace("{số điện thoại hỗ trợ}", invoiceEmail.SupportPhone ?? string.Empty);

                // Lưu URL của hóa đơn PDF (nếu có)
                if (!string.IsNullOrEmpty(invoiceEmail.InvoicePdfUrl))
                {
                    htmlBody = htmlBody.Replace("href=\"#\"", $"href=\"{invoiceEmail.InvoicePdfUrl}\"");
                }

                // Gán nội dung đã thay thế vào mailRequest
                invoiceEmail.Body = htmlBody;

                // Gửi email
                var email = new MimeMessage();
                email.Sender = MailboxAddress.Parse(_emailSettings.Email);
                email.To.Add(MailboxAddress.Parse(invoiceEmail.ToEmail));
                email.Subject = invoiceEmail.Subject ?? $"Cảm ơn quý khách - Hóa đơn #{invoiceEmail.InvoiceNumber}";

                var builder = new BodyBuilder { HtmlBody = invoiceEmail.Body };

                // Thêm logo vào email nếu có
                email.Body = builder.ToMessageBody();

                using var smtp = new SmtpClient();
                try
                {
                    smtp.Connect(_emailSettings.Host, _emailSettings.Port, SecureSocketOptions.StartTls);
                    smtp.Authenticate(_emailSettings.Email, _emailSettings.Password);
                    await smtp.SendAsync(email);
                }
                catch (SmtpCommandException ex)
                {
                    throw new InvalidOperationException($"Lỗi khi gửi email: {ex.StatusCode} - {ex.Message}");
                }
                catch (SmtpProtocolException ex)
                {
                    throw new InvalidOperationException($"Lỗi giao thức SMTP: {ex.Message}");
                }
                finally
                {
                    smtp.Disconnect(true);
                }
            }
            catch
            {
                throw;
            }
        }

    }
}
