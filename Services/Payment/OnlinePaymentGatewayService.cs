using Tech_Store.Models.DTO.Payment.Client.Momo;
using Tech_Store.Models.DTO.Payment.Client.VnPay;
using Tech_Store.Models.Enums;
using Tech_Store.Services.Admin.MomoServices;
using Tech_Store.Services.Admin.VNPayServices;

namespace Tech_Store.Services.Payment
{
    public class OnlinePaymentGatewayService : IOnlinePaymentGatewayService
    {
        private readonly IVnPayService _vnPayService;
        private readonly IMomoService _momoService;
        private readonly ISePayService _sePayService;

        public OnlinePaymentGatewayService(
            IVnPayService vnPayService,
            IMomoService momoService,
            ISePayService sePayService)
        {
            _vnPayService = vnPayService;
            _momoService = momoService;
            _sePayService = sePayService;
        }

        public async Task<OnlinePaymentGatewayStartResult> CreatePaymentUrlAsync(
            PaymentMethodType paymentMethodType,
            OnlinePaymentGatewayRequest request,
            HttpContext httpContext,
            CancellationToken cancellationToken = default)
        {
            switch (paymentMethodType)
            {
                case PaymentMethodType.Momo:
                    var momoPayModel = new MomoPaymentResquestModel
                    {
                        Amount = (double)request.Amount,
                        CreatedDate = request.CreatedDate,
                        Description = request.Description,
                        FullName = request.FullName,
                        OrderId = request.OrderId
                    };

                    var momoPayUrl = await _momoService.CreatePaymentUrl(momoPayModel);
                    return new OnlinePaymentGatewayStartResult
                    {
                        Success = true,
                        StatusCode = StatusCodes.Status200OK,
                        RedirectUrl = momoPayUrl.PayUrl
                    };

                case PaymentMethodType.VnPay:
                    var vnPayModel = new VnPaymentResquestModel
                    {
                        Amount = (double)request.Amount,
                        CreatedDate = request.CreatedDate,
                        Description = request.Description,
                        FullName = request.FullName,
                        OrderId = request.OrderId
                    };

                    return new OnlinePaymentGatewayStartResult
                    {
                        Success = true,
                        StatusCode = StatusCodes.Status200OK,
                        RedirectUrl = _vnPayService.CreatePaymentUrl(httpContext, vnPayModel)
                    };

                case PaymentMethodType.SePay:
                    return await _sePayService.CreatePaymentUrlAsync(request, httpContext, cancellationToken);

                default:
                    return new OnlinePaymentGatewayStartResult
                    {
                        Success = false,
                        StatusCode = StatusCodes.Status400BadRequest,
                        ErrorMessage = "Phương thức thanh toán không hợp lệ."
                    };
            }
        }

        public OnlinePaymentGatewayCallbackResult ValidateCallback(
            PaymentMethodType paymentMethodType,
            HttpContext httpContext)
        {
            switch (paymentMethodType)
            {
                case PaymentMethodType.Momo:
                    var momoResponse = _momoService.PaymentExecute(httpContext.Request.Query);
                    var momoResultCode = httpContext.Request.Query["resultCode"].ToString();

                    return momoResponse != null && (string.IsNullOrWhiteSpace(momoResultCode) || momoResultCode == "0")
                        ? new OnlinePaymentGatewayCallbackResult { Success = true }
                        : new OnlinePaymentGatewayCallbackResult
                        {
                            Success = false,
                            FailureMessage = $"Lỗi thanh toán MoMo: {momoResultCode}"
                        };

                case PaymentMethodType.VnPay:
                    var vnPayResponse = _vnPayService.PaymentExecute(httpContext.Request.Query);

                    return vnPayResponse != null && vnPayResponse.VnPayResponseCode == "00"
                        ? new OnlinePaymentGatewayCallbackResult { Success = true }
                        : new OnlinePaymentGatewayCallbackResult
                        {
                            Success = false,
                            FailureMessage = $"Lỗi thanh toán VNPay: {vnPayResponse?.VnPayResponseCode}"
                        };

                case PaymentMethodType.SePay:
                    return _sePayService.ValidateCallback(httpContext);

                default:
                    return new OnlinePaymentGatewayCallbackResult
                    {
                        Success = false,
                        FailureMessage = "Phương thức thanh toán không hợp lệ."
                    };
            }
        }
    }
}
