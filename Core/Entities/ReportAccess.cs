using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class ReportAccess
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public User User { get; set; }
        public int ReportId { get; set; }
        public Report Report { get; set; }
        public AccessLevel AccessLevel { get; set; }
    }

    public enum AccessLevel
    {
        None,       // Нет доступа
        Read,       // Только чтение
        Edit        // Чтение и редактирование
    }
}
