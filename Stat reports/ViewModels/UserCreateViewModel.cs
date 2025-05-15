using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Stat_reports.ViewModels
{
    public class UserCreateViewModel
    {
        [Required] public string UserName { get; set; }
        [Required] public string FullName { get; set; }
        public string? Number { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }
        [Required, MinLength(6)] public string Password { get; set; }

        public int RoleId { get; set; }
        public IEnumerable<SelectListItem> RoleOptions { get; set; }

        [Required] public int BranchId { get; set; }
        public IEnumerable<SelectListItem> BranchOptions { get; set; }
    }
}
