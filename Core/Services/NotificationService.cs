using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;
using Core.Interfaces;

namespace Core.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService(IUnitOfWork notificationRepository)
        {
            _unitOfWork = notificationRepository;
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
            }
        }

        public async Task<bool> DeleteNotification(int id)
        {
            var notification = await _unitOfWork.Notifications.FindAsync(n => n.Id == id);
            if (notification != null)
            {
                await _unitOfWork.Notifications.DeleteAsync(notification);
                return true;
            }
            return false;
        }
    }
}
