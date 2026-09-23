using Microsoft.Extensions.Options;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using YumQuick.Core.DTOs;
using YumQuick.Core.Entities;
using YumQuick.Core.Interfaces;

namespace YumQuick.Api.Services
{
    public class FawryPaymentService : IFawryPaymentService
    {
        private readonly FawrySettings _fawrySettings;
        private readonly IHttpClientFactory _httpClientFactory;

        public FawryPaymentService(IOptions<FawrySettings> fawrySettings, IHttpClientFactory httpClientFactory)
        {
            _fawrySettings = fawrySettings.Value;
            _httpClientFactory = httpClientFactory;
        }

        public string GenerateSignature(string merchantRefNum, string customerId, decimal amount)
        {
            // بناء النص المطلوب تشفيره حسب توثيق فوري
            // MerchantCode + MerchantRefNum + CustomerProfileId + Amount + SecurityKey
            string formattedAmount = amount.ToString("0.00"); // فوري تتطلب صيغة رقمية دقيقة

            string plainText = $"{_fawrySettings.MerchantCode}{merchantRefNum}{customerId}{formattedAmount}{_fawrySettings.SecurityKey}";

            return HashSHA256(plainText);
        }

        public bool ValidateWebhookSignature(string fawrySignature, string expectedDataStr)
        {
            var calculatedHash = HashSHA256(expectedDataStr);
            return string.Equals(fawrySignature, calculatedHash, StringComparison.OrdinalIgnoreCase);
        }

        private string HashSHA256(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                {
                    builder.Append(b.ToString("x2"));
                }
                return builder.ToString();
            }
        }
        public async Task<bool> ChargeTokenizedCardAsync(string merchantRefNum, string cardToken, decimal amount, string customerId)
        {
            var client = _httpClientFactory.CreateClient();
            string formattedAmount = amount.ToString("0.00");

            // حساب التوقيع (فوري تطلب توقيعاً للطلب حتى لو كان ببطاقة محفوظة)
            var signature = GenerateSignature(merchantRefNum, customerId, amount);

            var payload = new
            {
                merchantCode = _fawrySettings.MerchantCode,
                merchantRefNum = merchantRefNum,
                customerProfileId = customerId,
                paymentMethod = "CARD",
                amount = formattedAmount,
                cardToken = cardToken,
                signature = signature,
                description = "YumQuick Saved Card Payment"
            };

            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            // رابط بيئة الاختبار (Staging) الخاص بفوري
            var response = await client.PostAsync("https://atfawry.fawrystaging.com/fawrypay-api/api/payments/init", content);

            if (response.IsSuccessStatusCode)
            {
                var responseString = await response.Content.ReadAsStringAsync();
                using var jsonDoc = JsonDocument.Parse(responseString);
                var statusCode = jsonDoc.RootElement.GetProperty("statusCode").GetInt32();

                // 200 تعني نجاح العملية مبدئياً في فوري
                return statusCode == 200;
            }

            return false;
        }
    }
}