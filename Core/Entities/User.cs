using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class User:BaseEntity
    {
        public string UserName { get; set; }
        public string FullName { get; set; }
        public string? Number { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public int? AccessId {  get; set; }
        public string PasswordHash { get; set; }
        public int RoleId { get; set; }
        public SystemRole Role { get; set; } // Для пользователей с ролью "Администратор" или "Пользователь"
        public int? BranchId { get; set; } // Для пользователей филиалов
        public Branch? Branch { get; set; }
    }
}
