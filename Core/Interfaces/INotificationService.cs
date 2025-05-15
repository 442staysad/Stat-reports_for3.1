using System.Collections.Generic;
using System.Threading.Tasks;
using Core.Entities;

namespace Core.Interfaces
{
    public interface INotificationService
    {
        Task AddNotificationAsync(int userId, string message);
        Task<IEnumerable<Notification>> GetUserNotificationsAsync(int userId);
        Task MarkAsReadAsync(int notificationId);
        Task<bool> DeleteNotification(int id);
    }
}
