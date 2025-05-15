using Core.Enums;
using Core.DTO;
using System.Collections.Generic;

namespace Stat_reports.ViewModels
{
    public class ExcelPreviewViewModel
    {
        public string? BranchName { get; set; }
        public string? ReportType { get; set; }
        public int? DeadlineId { get; set; }
        public int ReportId { get; set; }
        public string ReportName { get; set; }
        public string FilePath { get; set; } // Исправляем тип
        public string? Comment { get; set; }
        public ReportStatus Status { get; set; }
        public List<CommentHistoryDto>?  CommentHistory{get; set;}
        public bool IsArchive { get; set; } // ➕
    }
}
