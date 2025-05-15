using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Entities
{
    public class SummaryReport : BaseEntity
    {
        public string Name { get; set; } // Название сводного отчета
        public string FilePath { get; set; } // Путь к файлу, куда сохраниться на сервере сводный отчет
        public DateTime CreatedDate { get; set; } // Дата формирования сводного отчета

        // Привязка к типу отчета (например, 4-ф, 6-т и т. д.)
        public int ReportTemplateId { get; set; }
        public ReportTemplate ReportTemplate { get; set; }

        // Период, за который сформирован сводный отчет
        public DateTime PeriodStart { get; set; }
        public DateTime PeriodEnd { get; set; }

        // Список отчетов, участвующих в сводном отчете
        public ICollection<Report> Reports { get; set; }
    }
}
