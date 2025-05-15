using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class RoleDto:BaseDTO
    {
        public string RoleName { get; set; } = null!;
        public string RoleNameRu { get; set; } = null!;
    }
}
