using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class Notification:BaseEntity
    {
        public int UserId { get; set; } // Пользователь, которому адресовано уведомление
        public User User { get; set; } // Пользователь, которому адресовано уведомление
        //public string Header { get; set; }
        public string Message { get; set; } // Текст уведомления
        public bool IsRead { get; set; } // Прочитано ли уведомление
        public DateTime CreatedAt { get; set; } // Дата создания уведомления
    }
}
