using System.Collections.Generic;
using Core.DTO;
using Core.Entities;
using Core.Enums;

namespace Stat_reports.ViewModels
{
    public class ReportArchiveViewModel
    {
       public IEnumerable<ReportDto> Reports { get; set; }
        public IEnumerable<Branch> Branches { get; set; }
        public IEnumerable<ReportTemplate> Templates { get; set; }
        public ReportFilterViewModel Filter { get; set; }
    }
}
