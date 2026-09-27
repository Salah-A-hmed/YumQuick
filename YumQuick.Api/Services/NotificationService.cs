using Microsoft.AspNetCore.SignalR;
using YumQuick.Api.Hubs;
using YumQuick.Core.Entities;
using YumQuick.Data;
using System.Threading.Tasks;

namespace YumQuick.Api.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(ApplicationDbContext context, IHubContext<NotificationHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async Task SendNotificationAsync(string userId, string title, string message)
        {
            // 1. حفظ الإشعار في الداتابيز
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            // 2. إرسال الإشعار لحظياً للموبايل لو المستخدم فاتح الأبلكيشن
            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.CreatedAt,
                notification.IsRead
            });
        }
    }
}