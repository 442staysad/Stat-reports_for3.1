using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class UserDto: BaseDTO
    {
        public string UserName { get; set; } = null!;
        public string FullName { get; set; } = null!;
        public string? Number { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        public int RoleId { get; set; }
        public string? RoleName { get; set; } = null!;

        public string? RoleNameRu { get; set; } = null!;
        public int BranchId { get; set; }
        public string BranchName { get; set; } = null!;
        public string Password { get; set; } = null!; // plain-text
    }
}
