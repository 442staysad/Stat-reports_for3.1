using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
namespace Core.Services
{

    public class NotificationHub : Hub
    {
        public async Task SendNotification(int userId, int unreadCount)
        {
            await Clients.User(userId.ToString()).SendAsync("ReceiveNotificationCount", unreadCount);
        }
    }
}
