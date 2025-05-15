using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using DocumentFormat.OpenXml.Drawing.Charts;

namespace Core.Services
{
    public class ReportTemplateService : IReportTemplateService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBranchService _branchService;
        private readonly IDeadlineService _deadlineService;
        private readonly IFileService _fileService;

        public ReportTemplateService(IUnitOfWork unitOfWork,
            IFileService fileService,
            IDeadlineService deadlineService,
            IBranchService branchService)
        {
            _unitOfWork = unitOfWork;
            _fileService = fileService;
            _deadlineService = deadlineService;
            _branchService = branchService;
        }

        public async Task<SubmissionDeadline> CreateSubmissionDeadlineAsync(SubmissionDeadline deadline)
        {
            await _unitOfWork.SubmissionDeadlines.AddAsync(deadline);
            return deadline;
        }

        public async Task<IEnumerable<ReportTemplate>> GetAllReportTemplatesAsync()
        {
            return await _unitOfWork.ReportTemplates.GetAllAsync();
        }

        public async Task<ReportTemplate> GetReportTemplateByIdAsync(int id)
        {
            return await _unitOfWork.ReportTemplates.FindAsync(r => r.Id == id);
        }

        public async Task<ReportTemplate> CreateReportTemplateAsync(ReportTemplate template,DeadlineType deadlineType,int FixedDay, DateTime ReportDate)
        {
            var branches = await _branchService.GetAllBranchesAsync();
            await _unitOfWork.ReportTemplates.AddAsync(template);

            foreach (var branch in branches)
            {
                // Вычисление дедлайна, используя дату отчета
                var deadlineDate = _deadlineService.CalculateDeadline(deadlineType, FixedDay, ReportDate);

                // Создание записи о дедлайне
                var deadline = new SubmissionDeadline
                {
                    BranchId = branch.Id,
                    Branch = branch,
                    ReportTemplateId = template.Id,
                    Template = template,
                    DeadlineType = deadlineType,
                    DeadlineDate = deadlineDate,
                    IsClosed = false,
                    FixedDay = FixedDay,
                    
                };
                switch (deadlineType)
                {
                    case DeadlineType.Monthly : deadline.Period = ReportDate.AddMonths(1); break;
                    case DeadlineType.Quarterly : deadline.Period = ReportDate.AddMonths(3); break;
                    case DeadlineType.HalfYearly: deadline.Period = ReportDate.AddMonths(6); break;
                    case DeadlineType.Yearly: deadline.Period = ReportDate.AddYears(1); break;
                }
                // Сохранение дедлайна в базе данных
                await _unitOfWork.SubmissionDeadlines.AddAsync(deadline);
            }
            return template;
        }

        public async Task<ReportTemplate> UpdateReportTemplateAsync(ReportTemplate template)
        {
            await _unitOfWork.ReportTemplates.UpdateAsync(template);
            return template;
        }

        public async Task<bool> DeleteReportTemplateAsync(int id)
        {
            var template = await _unitOfWork.ReportTemplates.FindAsync(r => r.Id == id);
            if (template == null) return false;
            await _unitOfWork.ReportTemplates.DeleteAsync(template);
            await _fileService.DeleteFileAsync(template.FilePath);
            return true;
        }
    }
}
