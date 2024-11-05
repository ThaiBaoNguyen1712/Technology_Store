using Tech_Store.Models.DTO;

namespace Tech_Store.Features.Payment.Commands
{
    public class CreatePayment/* : IRequest<BaseResultWithData<PaymentLinkDTo>>*/
    {
        public string PaymentContent { get; set; }

    }
}
