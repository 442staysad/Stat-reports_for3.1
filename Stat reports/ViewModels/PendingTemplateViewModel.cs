using System;
using Core.Enums;

namespace Stat_reports.ViewModels
{
    public class PendingTemplateViewModel
    {
        public int? DeadlineId { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string ReportType {  get; set; }
        public DateTime Deadline { get; set; }
        public DateTime Period { get; set; }
        public DeadlineType Type { get; set; } // Тип дедлайна (ежемесячный, квартальный и т.д.)
        public ReportStatus Status { get; set; }
        public string? Comment { get; set; }
        public int? CommentId { get; set; } // Идентификатор комментария, если есть
        public int? LatestCommentAuthorId { get; set; }
        public int? ReportId { get; set; }
        public int BranchId { get; set; } 
        public string BranchName { get; set; } 
        public string ReportTypeName { get; set; }

    }
}
