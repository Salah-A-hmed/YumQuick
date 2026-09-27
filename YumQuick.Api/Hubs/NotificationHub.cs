using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace YumQuick.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
    }
}