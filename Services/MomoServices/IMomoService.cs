using Tech_Store.Models.DTO.Payment.Client;
using Tech_Store.Models.DTO.Payment.Client.Momo;
using Tech_Store.Models.DTO.Payment.Client.VnPay;

namespace Tech_Store.Services.MomoServices
{
    public interface IMomoService
    {
        Task<MomoPaymentResponseModel> CreatePaymentUrl(MomoPaymentResquestModel model);
        MomoExecuteResponseModel PaymentExecute(IQueryCollection collections);
    }
}
