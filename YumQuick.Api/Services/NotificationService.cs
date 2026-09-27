using FirebaseAdmin.Messaging;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using YumQuick.Api.Hubs;
using YumQuick.Core.Entities;
using YumQuick.Data;
using System.Threading.Tasks;
using System.Linq;

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
            var notification = new YumQuick.Core.Entities.Notification
            {
                UserId = userId,
                Title = title,
                Message = message
            };

            _context.Notifications.Add(notification);
            await _context.SaveChangesAsync();

            await _hubContext.Clients.User(userId).SendAsync("ReceiveNotification", new
            {
                notification.Id,
                notification.Title,
                notification.Message,
                notification.CreatedAt,
                notification.IsRead
            });

            var userTokens = await _context.DeviceTokens
                .Where(t => t.UserId == userId)
                .Select(t => t.Token)
                .ToListAsync();

            if (userTokens.Any())
            {
                var fcmMessage = new MulticastMessage
                {
                    Tokens = userTokens,
                    Notification = new FirebaseAdmin.Messaging.Notification
                    {
                        Title = title,
                        Body = message
                    },
                    Data = new Dictionary<string, string>
                    {
                        { "click_action", "FLUTTER_NOTIFICATION_CLICK" },
                        { "notificationId", notification.Id.ToString() }
                    }
                };

                try
                {
                    var response = await FirebaseMessaging.DefaultInstance.SendMulticastAsync(fcmMessage);

                    if (response.FailureCount > 0)
                    {
                        for (int i = 0; i < response.Responses.Count; i++)
                        {
                            if (!response.Responses[i].IsSuccess)
                            {
                                var failedToken = userTokens[i];
                                var tokenToDelete = await _context.DeviceTokens.FirstOrDefaultAsync(t => t.Token == failedToken);
                                if (tokenToDelete != null)
                                {
                                    _context.DeviceTokens.Remove(tokenToDelete);
                                }
                            }
                        }
                        await _context.SaveChangesAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error sending FCM notification: {ex.Message}");
                }
            }
        }
    }
}