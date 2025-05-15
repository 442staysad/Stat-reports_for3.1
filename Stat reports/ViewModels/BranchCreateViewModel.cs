using System.ComponentModel.DataAnnotations;

namespace Stat_reports.ViewModels
{
    public class BranchCreateViewModel
    {
        [Required] public string Name { get; set; }
        public string? Shortname { get; set; }
        [Required] public string UNP { get; set; }
        public string? OKPO { get; set; }
        public string? OKYLP { get; set; }
        public string? Region { get; set; }
        public string? Address { get; set; }
        [EmailAddress] public string? Email { get; set; }
        public string? GoverningName { get; set; }
        public string? HeadName { get; set; }
        public string? Supervisor { get; set; }
        public string? ChiefAccountant { get; set; }
        [Required, MinLength(6)] public string Password { get; set; }
    }
}
