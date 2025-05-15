using System;

namespace Core.DTO
{
    public class SummaryReportDto
    {
        public int Id { get; set; }
        public string Name { get; set; } // Название сводного отчета
        public string FilePath { get; set; } // Путь к файлу
        public DateTime CreatedDate { get; set; } // Дата создания
        public int ReportTemplateId { get; set; } // Тип отчета
        public DateTime PeriodStart { get; set; } // Начало периода
        public DateTime PeriodEnd { get; set; } // Конец периода
    }
}