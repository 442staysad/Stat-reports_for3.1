namespace Stat_reports.ViewModels
{
    public class UserProfileViewModel
    {
        // User data
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string? Number { get; set; }
        public string? Email { get; set; }
        public string? Position { get; set; }

        // Branch data
        public int BranchId { get; set; }
        public string? Name { get; set; }
        public string? Shortname { get; set; }
        public string? UNP { get; set; }
        public string? OKPO { get; set; }
        public string? OKYLP { get; set; }
        public string? Region { get; set; }
        public string? Address { get; set; }
        public string? BranchEmail { get; set; }
        public string? GoverningName { get; set; }
        public string? HeadName { get; set; }
        public string? Supervisor { get; set; }
        public string? ChiefAccountant { get; set; }
    }
}
