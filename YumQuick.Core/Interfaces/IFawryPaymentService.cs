using System;
using System.Collections.Generic;
using System.Text;

namespace YumQuick.Core.Interfaces
{
    public interface IFawryPaymentService
    {
        string GenerateSignature(string merchantRefNum, string customerId, decimal amount);
        bool ValidateWebhookSignature(string fawrySignature, string expectedDataStr);
        Task<bool> ChargeTokenizedCardAsync(string merchantRefNum, string cardToken, decimal amount, string customerId);
    }
}
