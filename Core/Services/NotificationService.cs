using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IHubContext<NotificationHub> _hubContext;

        public NotificationService(IUnitOfWork notificationRepository, IHubContext<NotificationHub> hubContext)
        {
            _unitOfWork = notificationRepository;
            _hubContext = hubContext;
        }

        public async Task AddNotificationAsync(int userId, string message)
        {
            var notification = new Notification
            {
                UserId = userId,
                Message = message,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            var unreadCount = (await GetUserNotificationsAsync(notification.UserId)).Count();
            await _hubContext.Clients.User(userId.ToString()).SendAsync("ReceiveNotificationCount", unreadCount);
        }

        public async Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId)
        {
            return await _unitOfWork.Notifications
                .FindAllAsync(n => n.UserId == userId && !n.IsRead);

        }

        public async Task MarkAsReadAsync(int notificationId)
        {
            var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                await _unitOfWork.Notifications.UpdateAsync(notification);
                var unreadCount = (await GetUserNotificationsAsync(notification.UserId)).Count();
                await _hubContext.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotificationCount", unreadCount);
            }
        }

        public async Task<bool> DeleteNotification(int id)
        {
            var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == id);
            if (notification != null)
            {
                await _unitOfWork.Notifications.DeleteAsync(notification);
                var unreadCount = (await GetUserNotificationsAsync(notification.UserId)).Count();
                await _hubContext.Clients.User(notification.UserId.ToString()).SendAsync("ReceiveNotificationCount", unreadCount);
                return true;
            }

            return false;
        }
    }
}
