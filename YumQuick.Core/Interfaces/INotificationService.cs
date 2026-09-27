using System.Threading.Tasks;

namespace YumQuick.Api.Services
{
    public interface INotificationService
    {
        Task SendNotificationAsync(string userId, string title, string message);
    }
}