using Tech_Store.Models.DTO.Payment.Client.VnPay;

namespace Tech_Store.Services.VNPayServices
{
    public interface IVnPayService
    {
        string CreatePaymentUrl(HttpContext context,VnPaymentResquestModel model);
        VNPaymentResponseModel PaymentExecute(IQueryCollection collection);
    }
}
