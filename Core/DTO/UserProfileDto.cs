using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.DTO
{
    public class UserProfileDto:BaseDTO
    {
        public string FullName { get; set; }
        public string? Number { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
    }
}
