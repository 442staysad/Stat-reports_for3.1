using System;
using Core.Enums;

namespace Core.Entities
{
    public class Report : BaseEntity
    {
        public string Name { get; set; }
        public int TemplateId { get; set; }
        public ReportTemplate Template { get; set; }
        public DateTime UploadDate { get; set; }
        public DateTime Period { get; set; }

        public int? UploadedById { get; set; }
        public User? UploadedBy { get; set; }

        public int? BranchId { get; set; }
        public Branch? Branch { get; set; }

        public string FilePath { get; set; }//название отчета

        public ReportType Type { get; set; } // Тип отчета (план/факт)
        public bool IsClosed { get; set; } // Закрыт ли отчет
    }
}
