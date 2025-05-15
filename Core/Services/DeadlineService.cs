using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Core.Services
{
    public class DeadlineService : IDeadlineService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<DeadlineService> _logger;
        private readonly IFileService _fileService;

        public DeadlineService(IUnitOfWork unitOfWork, ILogger<DeadlineService> logger, IFileService fileService)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
            _fileService = fileService;
        }

        public async Task<SubmissionDeadline> GetDeadlineByIdAsync(int id)
        {
            return await _unitOfWork.SubmissionDeadlines.FindAsync(r => r.Id == id);
        }

        public async Task<IEnumerable<SubmissionDeadline>> GetAllAsync() => 
            await _unitOfWork.SubmissionDeadlines.GetAll(q => 
                                                         q.Include(r => r.Template)
                                                          .Include(b => b.Branch))
                                                          .ToListAsync();

        public async Task CheckAndUpdateDeadlineAsync(int templateId, int branchId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var repo = _unitOfWork.SubmissionDeadlines;

                // Находим последний закрытый дедлайн для данного шаблона и филиала
                var lastDeadline = await repo.FindAsync(
                    d => d.ReportTemplateId == templateId &&
                         d.BranchId == branchId &&
                         d.IsClosed,
                    includes: q => q.Include(d => d.Template)
                                  .Include(d => d.Branch));

                if (lastDeadline == null)
                {
                    _logger.LogWarning($"Не найден закрытый дедлайн для templateId: {templateId}, branchId: {branchId}");
                    return;
                }

                // Проверяем, что отчет был принят (статус Reviewed)
                if (lastDeadline.Status != ReportStatus.Reviewed)
                {
                    _logger.LogWarning($"Последний дедлайн не имеет статус Reviewed для templateId: {templateId}, branchId: {branchId}");
                    return;
                }

                // Создаем новый дедлайн на основе предыдущего
                var newDeadline = new SubmissionDeadline
                {
                    BranchId = branchId,
                    ReportTemplateId = templateId,
                    DeadlineType = lastDeadline.DeadlineType,
                    FixedDay = lastDeadline.FixedDay,
                    Status = ReportStatus.InProgress,
                    IsClosed = false,
                    DeadlineDate = CalculateNextDeadline(lastDeadline),
                    Period = CalculateNextPeriod(lastDeadline)
                };

                // Закрываем предыдущий дедлайн
                lastDeadline.IsClosed = true;

                await repo.UpdateAsync(lastDeadline);
                await repo.AddAsync(newDeadline);

                await transaction.CommitAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при создании нового дедлайна");
                await transaction.RollbackAsync();
                throw;
            }
        }

        private DateTime AdjustDate(DateTime date, int fixedDay)
        {
            int daysInMonth = DateTime.DaysInMonth(date.Year, date.Month);
            return new DateTime(date.Year, date.Month, Math.Min(fixedDay, daysInMonth));
        }

        private DateTime CalculateNextDeadline(SubmissionDeadline deadline)
        {
            return deadline.DeadlineType switch
            {
                DeadlineType.Monthly => AdjustDate(deadline.DeadlineDate.AddMonths(1), deadline.FixedDay ?? 30),
                DeadlineType.Quarterly => AdjustDate(deadline.DeadlineDate.AddMonths(3), deadline.FixedDay ?? 30),
                DeadlineType.HalfYearly => AdjustDate(deadline.DeadlineDate.AddMonths(6), deadline.FixedDay ?? 30),
                DeadlineType.Yearly => AdjustDate(deadline.DeadlineDate.AddYears(1), deadline.FixedDay ?? 30),
                _ => throw new ArgumentOutOfRangeException(nameof(deadline.DeadlineType))
            };
        }

        private DateTime CalculateNextPeriod(SubmissionDeadline deadline)
        {
            return deadline.DeadlineType switch
            {
                DeadlineType.Monthly => deadline.Period.AddMonths(1),
                DeadlineType.Quarterly => deadline.Period.AddMonths(3),
                DeadlineType.HalfYearly => deadline.Period.AddMonths(6),
                DeadlineType.Yearly => deadline.Period.AddYears(1),
                _ => throw new ArgumentOutOfRangeException(nameof(deadline.DeadlineType))
            };
        }

        public DateTime CalculateDeadline(DeadlineType deadlineType, int fixedDay, DateTime reportDate)
        {
            return deadlineType switch
            {
                DeadlineType.Monthly => AdjustDate(reportDate.AddMonths(1), fixedDay),
                DeadlineType.Quarterly => AdjustDate(reportDate.AddMonths(3 - (reportDate.Month - 1) % 3), fixedDay),
                DeadlineType.HalfYearly => AdjustDate(reportDate.AddMonths(6 - (reportDate.Month - 1) % 6), fixedDay),
                DeadlineType.Yearly => AdjustDate(reportDate.AddYears(1), fixedDay),
                _ => throw new ArgumentOutOfRangeException(nameof(deadlineType))
            };
        }

        public async Task<bool> DeleteDeadlineAsync(int id)
        {
            var deadline = await _unitOfWork.SubmissionDeadlines.FindAsync(r => r.Id == id);
            var report = await _unitOfWork.Reports.FindAsync(r => r.Id== deadline.ReportId);
            if (deadline== null) return false;
            await _unitOfWork.SubmissionDeadlines.DeleteAsync(deadline);
            await _fileService.DeleteFileAsync(report.FilePath);
            return true;
        }

        /*
        private DateTime CalculateNextDeadline(SubmissionDeadline deadline)
        {
            return deadline.DeadlineType switch
            {
                DeadlineType.Monthly => AdjustDate(deadline.DeadlineDate.AddMonths(1), deadline.FixedDay ?? 30),
                DeadlineType.Quarterly => AdjustDate(deadline.DeadlineDate.AddMonths(3 - (deadline.DeadlineDate.Month - 1) % 3), deadline.FixedDay ?? 30),
                DeadlineType.HalfYearly => AdjustDate(deadline.DeadlineDate.AddMonths(6 - (deadline.DeadlineDate.Month - 1) % 6), deadline.FixedDay ?? 30),
                DeadlineType.Yearly => AdjustDate(deadline.DeadlineDate.AddYears(1), deadline.FixedDay ?? 30),
                _ => DateTime.UtcNow
            };
        }*/
    }
}
