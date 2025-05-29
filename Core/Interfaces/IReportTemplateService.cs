using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;

namespace Core.Interfaces
{
    public interface IReportTemplateService
    {
        Task<ReportTemplate> CreateReportTemplateAsync(ReportTemplate template, 
            DeadlineType deadlineType, int FixedDay, DateTime ReportDate);
        Task<SubmissionDeadline> CreateSubmissionDeadlineAsync(SubmissionDeadline deadline);
        Task<bool> DeleteReportTemplateAsync(int id);
        Task<IEnumerable<ReportTemplate>> GetAllReportTemplatesAsync();
        Task<ReportTemplate> GetReportTemplateByIdAsync(int id);
        Task<ReportTemplate> UpdateReportTemplateAsync(ReportTemplate template);

    }
}
