using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Core.Entities
{
    public class SystemRole : BaseEntity
    {
        [Required]
        public string RoleName { get; set; }
        public string RoleNameRu { get; set; }
    }
}
