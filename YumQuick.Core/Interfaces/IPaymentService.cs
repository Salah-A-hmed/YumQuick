using YumQuick.Core.Entities;
using System.Threading.Tasks;

namespace YumQuick.Core.Interfaces
{
    public interface IPaymentService
    {
        Task<string> CreatePaymentIntentAsync(Order order, string? paymentMethodToken = null);
    }
}