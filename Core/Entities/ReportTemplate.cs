using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Enums;

namespace Core.Entities
{
    public class ReportTemplate : BaseEntity
    {
        public string Name { get; set; }
        public string? Description { get; set; }
        public DeadlineType DeadlineType { get; set; }
        public string FilePath { get; set; }//ЭТО ФАЙЛ НАДО БУДЕТ ЗАГРУЖАТЬ  
        public ReportType Type { get; set; } // Тип отчета (план/факт)
        public ICollection<Report>? Reports { get; set; }
        public ICollection<SubmissionDeadline>? Deadlines { get; set; }
    }
}

