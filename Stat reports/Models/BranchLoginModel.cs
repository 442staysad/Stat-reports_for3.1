using System.ComponentModel.DataAnnotations;

namespace Stat_reports.Models
{
    public class BranchLoginModel
    {
        [Required]
        public string UNP { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }
    }
}