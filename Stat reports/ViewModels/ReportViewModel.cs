using System;
using Core.Entities;
using Core.Enums;

namespace Stat_reports.ViewModels
{
    public class ReportViewModel
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public DateTime SubmissionDate { get; set; }
        public string? UploadedBy { get; set; } // Имя пользователя
        public string? Branch { get; set; } // Название филиала
        public string? Template { get; set; } // Название шаблона отчета
        public ReportStatus Status { get; set; }
        public string? FilePath { get; set; }
    }
}