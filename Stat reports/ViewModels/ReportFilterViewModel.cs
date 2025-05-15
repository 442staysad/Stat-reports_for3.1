using Core.Enums;

namespace Stat_reports.ViewModels
{
    public class ReportFilterViewModel
    {
        public string? Name { get; set; }
        public int? TemplateId { get; set; }
        public int? BranchId { get; set; }

        // Новые поля для фильтрации по периоду
        public int? Year { get; set; }
        public int? Month { get; set; }
        public int? Quarter { get; set; }
        public int? HalfYearPeriod { get; set; }

        // Удаляем StartDate и EndDate
        // public DateTime? StartDate { get; set; }
        // public DateTime? EndDate { get; set; }

        public ReportType? Type { get; set; }
    }
}
