using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Reflection.Metadata.Ecma335;
using System.Security.Cryptography;
using System.Text;
using Tech_Store.Helpers;
using Tech_Store.Models.DTO.Payment.Client.Momo;
using Tech_Store.Models.DTO.Payment.Client.VnPay;

namespace Tech_Store.Services.Admin.MomoServices
{
    public class MomoService : IMomoService
    {
        private readonly IConfiguration _config;
        public MomoService(IConfiguration config)
        {
            _config = config;
        }
        public async Task<MomoPaymentResponseModel> CreatePaymentUrl(MomoPaymentResquestModel model)
        {
            try
            {
                model.Description = $"Khách hàng: {model.FullName}. Ngày thanh toán: {model.CreatedDate:dd/MM/yyyy}";

                var rawData =
                    $"accessKey={_config["MomoPay:AccessKey"]}" +
                    $"&amount={model.Amount}" +
                    $"&extraData=" +
                    $"&ipnUrl={_config["MomoPay:NotifyUrl"]}" +
                    $"&orderId={model.OrderId}" +
                    $"&orderInfo={model.Description}" +
                    $"&partnerCode={_config["MomoPay:PartnerCode"]}" +
                    $"&redirectUrl={_config["MomoPay:ReturnUrl"]}" +
                    $"&requestId={model.OrderId}" +
                    $"&requestType={_config["MomoPay:RequestType"]}";

                var signature = ComputeHmacSha256(rawData, _config["MomoPay:SecretKey"]);

                var requestData = new
                {
                    partnerCode = _config["MomoPay:PartnerCode"],
                    accessKey = _config["MomoPay:AccessKey"],
                    requestId = model.OrderId,
                    amount = model.Amount,
                    orderId = model.OrderId,
                    orderInfo = model.Description,
                    redirectUrl = _config["MomoPay:ReturnUrl"],
                    ipnUrl = _config["MomoPay:NotifyUrl"],
                    requestType = _config["MomoPay:RequestType"],
                    extraData = "",
                    lang = "vi",
                    signature
                };

                using var httpClient = new HttpClient();
                var jsonRequest = JsonConvert.SerializeObject(requestData);
                var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");

                var response = await httpClient.PostAsync(_config["MomoPay:MomoApiUrl"], content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    throw new Exception($"Lỗi khi gửi yêu cầu thanh toán đến Momo: {responseContent}");
                }

                var paymentResponse = JsonConvert.DeserializeObject<MomoPaymentResponseModel>(responseContent);

                if (paymentResponse == null)
                {
                    throw new Exception("Không thể parse phản hồi từ Momo");
                }

                return paymentResponse;
            }
            catch (Exception ex)
            {
                throw new Exception($"Lỗi xử lý thanh toán Momo: {ex.Message}", ex);
            }
        }

        private string ComputeHmacSha256(string message, string secretKey)
        {
            if (string.IsNullOrEmpty(message) || string.IsNullOrEmpty(secretKey))
            {
                throw new ArgumentNullException("message or secretKey", "Values for HMAC computation cannot be null or empty.");
            }

            var keyBytes = Encoding.UTF8.GetBytes(secretKey);
            var messageBytes = Encoding.UTF8.GetBytes(message);

            using (var hmac = new HMACSHA256(keyBytes))
            {
                var hashBytes = hmac.ComputeHash(messageBytes);
                return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
            }
        }


        public MomoExecuteResponseModel PaymentExecute(IQueryCollection collections)
        {
            var amount = collections.First(s => s.Key == "amount").Value;
            var orderInfo = collections.First(s => s.Key == "orderInfo").Value;
            var orderId = collections.First(s => s.Key == "orderId").Value;

            return new MomoExecuteResponseModel
            {
                OrderId = orderId,
                Amount = amount,
                OrderInfo = orderInfo
            };
        }
    }
}
