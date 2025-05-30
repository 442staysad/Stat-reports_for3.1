using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;

namespace Core.Services
{
    public class ReportService : IReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly INotificationService _notificationService;
        private readonly IFileService _fileService;
        private readonly IDeadlineService _deadlineService;

        public ReportService(
            IUnitOfWork unitOfWork,
            INotificationService notificationService,
            IFileService fileService,
            IDeadlineService deadlineService)
        {
            _unitOfWork = unitOfWork;
            _notificationService = notificationService;
            _fileService = fileService;
            _deadlineService = deadlineService;
        }

        public async Task<IEnumerable<Report>> GetAllReportsAsync()
        {
            return await _unitOfWork.Reports.GetAllAsync();
        }

        public async Task<IEnumerable<Report>> GetReportsByBranchAsync(int branchId)
        {
            return await _unitOfWork.Reports.FindAllAsync(r => r.BranchId == branchId);
        }

        public async Task<ReportDto?> GetReportByIdAsync(int id)
        {
            var report = await _unitOfWork.Reports.FindAsync(r => r.Id == id);

            if (report == null)
                return null;

            var reportDto = MapToDto(report);

            var comments = await _unitOfWork.CommentHistory
                .FindAll(query => query.Where(c => c.ReportId == id).Include(c => c.Author))
                .ToListAsync();

            reportDto.CommentHistory = comments.Select(c => new CommentHistoryDto
            {
                Comment = c.Comment,
                CreatedAt = c.CreatedAt,
                AuthorFullName = c.Author != null ? $"{c.Author.FullName}" : "Неизвестный автор",
                Id = c.Id, // Если нужен ID записи истории
                DeadlineId = c.DeadlineId ?? 0 // Используем ?? для избежания null

            }).ToList();

            return reportDto;
        }

        public async Task<ReportDto> CreateReportAsync(ReportDto reportDto)
        {
            var report = MapToEntity(reportDto);
            var created = await _unitOfWork.Reports.AddAsync(report);
            return MapToDto(created);
        }

        public async Task<Report?> FindByTemplateBranchPeriodAsync(int templateId, int branchId, int year, int month)
        {
            return await _unitOfWork.Reports.FindAsync(r =>
                r.TemplateId == templateId &&
                r.BranchId == branchId &&
                r.Period.Year == year &&
                r.Period.Month == month);
        }

        public async Task<ReportDto> UpdateReportAsync(int id, ReportDto reportDto)
        {
            var report = await _unitOfWork.Reports.FindAsync(r => r.Id == id);
            if (report == null) return null;

            report.Name = reportDto.Name;
            report.UploadDate = reportDto.SubmissionDate;
            report.FilePath = reportDto.FilePath;

            await _unitOfWork.Reports.UpdateAsync(report);

            return MapToDto(report);
        }

        public async Task<bool> DeleteReportAsync(int id)
        {
            var report = await _unitOfWork.Reports.FindAsync(r => r.Id == id);
            if (report == null) return false;

            if (!string.IsNullOrEmpty(report.FilePath))
                await _fileService.DeleteFileAsync(report.FilePath);

            await _unitOfWork.Reports.DeleteAsync(report);

            return true;
        }

        public async Task<ReportDto> UploadReportAsync(int templateId, int branchId, int uploadedById, IFormFile file,int deadlineId)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Файл отсутствует или пуст");

            var branch = await _unitOfWork.Branches.FindAsync(b => b.Id == branchId)
                         ?? throw new ArgumentException("Филиал не найден");

            var template = await _unitOfWork.ReportTemplates.FindAsync(t => t.Id == templateId)
                           ?? throw new ArgumentException("Шаблон не найден");

            var deadline = await _unitOfWork.SubmissionDeadlines.FindAsync(d =>d.Id==deadlineId)
                ?? throw new ArgumentException("Срок сдачи не найден");

            var existingReport = await _unitOfWork.Reports.FindAsync(r =>
                r.Id==deadline.ReportId);


            if(existingReport != null)
{
                if (!string.IsNullOrEmpty(existingReport.FilePath))
                    await _fileService.DeleteFileAsync(existingReport.FilePath); // <-- 1. Удаляем старый файл

                var filePath1 = await _fileService.SaveFileAsync(file, "Reports", branch.Name, DateTime.Now.Year, template.Name); // <-- 2. Сохраняем новый файл (ЭТО НУЖНО СДЕЛАТЬ ЗДЕСЬ)

                existingReport.Name = Path.GetFileNameWithoutExtension(file.FileName);
                existingReport.FilePath = filePath1; // <-- 3. Обновляем путь к файлу в базе данных
                existingReport.UploadedById = uploadedById;
                existingReport.UploadDate = DateTime.UtcNow;
                existingReport.Period = deadline.Period;

                await _unitOfWork.Reports.UpdateAsync(existingReport); // <-- 4. Обновляем запись отчета в БД

                deadline.Status = ReportStatus.Draft;
                deadline.ReportId = existingReport.Id;
                await _unitOfWork.SubmissionDeadlines.UpdateAsync(deadline);

                return MapToDto(existingReport);
            }

            var filePath = await _fileService.SaveFileAsync(file, "Reports", branch.Name, DateTime.Now.Year, template.Name);
            var newReport = new Report
            {
                Name = Path.GetFileNameWithoutExtension(file.FileName),
                TemplateId = templateId,
                BranchId = branchId,
                UploadedById = uploadedById,
                FilePath = filePath,
                UploadDate = DateTime.UtcNow,
                Period = deadline.Period,
                Branch = branch,
                Template = template
            };

            var createdReport = await _unitOfWork.Reports.AddAsync(newReport);

            deadline.Status = ReportStatus.Draft;
            deadline.ReportId = createdReport.Id;
            await _unitOfWork.SubmissionDeadlines.UpdateAsync(deadline);

            var user = await _unitOfWork.Users.GetAll(u => u.Include(r => r.Role))
                .Where(u => u.Id == uploadedById).FirstOrDefaultAsync();
                      

            return MapToDto(createdReport);
        }
        
        public async Task<bool> UpdateReportStatusAsync(int deadlineId, int reportId, ReportStatus newStatus)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                
                var report = await _unitOfWork.Reports.FindAsync(r => r.Id == reportId);
                
                // Находим активный (не закрытый) дедлайн для этого отчета
                var deadline = await _unitOfWork.SubmissionDeadlines.FindAsync(
                    d => d.Id==deadlineId,
                    includes: q => q.Include(d => d.Branch));

                if (deadline == null) return false;

                deadline.Status = newStatus;
                //коммент

                if (newStatus == ReportStatus.Reviewed)
                {
                    // Помечаем текущий дедлайн как закрытый
                    deadline.IsClosed = true;
                    deadline.ReportId = reportId; // Связываем с отчетом
                    report.IsClosed = true; // Закрываем отчет
                    await _unitOfWork.Reports.UpdateAsync(report);
                    await _unitOfWork.SubmissionDeadlines.UpdateAsync(deadline);
                    // Создаем новый дедлайн вместо обновления
                    if (!deadline.Reopened)
                    {
                        await _deadlineService.CheckAndUpdateDeadlineAsync(
                            report.TemplateId,
                            (int)report.BranchId, reportId);
                    }
                    deadline.Reopened = false; // Сбрасываем флаг повторного открытия
                    var users = await _unitOfWork.Users.FindAllAsync(
                        u => u.BranchId == report.BranchId);

                    await _notificationService.AddNotificationAsync(
                        (int)report.UploadedById,
                        $"Отчет '{report.Name}' за период '{report.Period:yyyy-MM-dd}' был принят.");
                }

                await _unitOfWork.SubmissionDeadlines.UpdateAsync(deadline);
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<bool> AddReportCommentAsync(int deadlinId, int reportId, string comment, int? authorId)
        {
            using var transaction = await _unitOfWork.BeginTransactionAsync();
            try
            {
                var report = await _unitOfWork.Reports.FindAsync(r => r.Id == reportId);
                if (report == null) return false;

                var deadline = await _unitOfWork.SubmissionDeadlines.FindAsync(
                    d => d.Id==deadlinId);

                if (deadline == null) return false;

                deadline.Status = ReportStatus.NeedsCorrection;

                var commentHistory = new CommentHistory
                {
                    Comment = comment,
                    CreatedAt = DateTime.Now,
                    DeadlineId = deadline.Id,
                    ReportId = deadline.ReportId,
                    AuthorId = authorId
                };
                var author = await _unitOfWork.Users.FindAsync(u => u.Id == authorId);
                await _unitOfWork.CommentHistory.AddAsync(commentHistory);

                await _notificationService.AddNotificationAsync(
                    (int)report.UploadedById,
                    $"{author.FullName}:  {report.Name}: {comment}");

                await _unitOfWork.SubmissionDeadlines.UpdateAsync(deadline);
                await transaction.CommitAsync();

                return true;
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<byte[]> GetReportFileAsync(int reportId)
        {
            var report = await _unitOfWork.Reports.FindAsync(r => r.Id == reportId);
            if (report == null || string.IsNullOrEmpty(report.FilePath))
                throw new FileNotFoundException("Файл отчета не найден");

            return await _fileService.GetFileAsync(report.FilePath);
        }

        public async Task<List<PendingTemplateDto>> GetPendingTemplatesAsync(int? branchId)
        {
            var query = _unitOfWork.SubmissionDeadlines
                .GetAll(q => q
                    .Include(t => t.Template)
                    .Include(d => d.CommentHistory) // Подгружаем историю комментариев
                        .ThenInclude(ch => ch.Author)); // <-- ДОБАВЛЕНО: Подгружаем автора для каждого CommentHistory

            if (branchId.HasValue)
                query = query.Where(d => d.BranchId == branchId.Value && !d.IsClosed);

            var deadlines = await query.ToListAsync();

            return deadlines.Select(d => new PendingTemplateDto
            {
                Id = d.Id,
                TemplateId = d.ReportTemplateId,
                TemplateName = d.Template?.Name ?? "Неизвестный шаблон",
                Deadline = d.DeadlineDate,
                ReportId = d.ReportId,
                Status = (ReportStatus)d.Status,
                Comment = d.Comment,
                ReportType = d.Template?.Type.ToString(),
                Period = d.Period,
                Type = (DeadlineType)(d.Template?.DeadlineType),// Используем DeadlineType из шаблона
                BranchId = d.BranchId,
                CommentHistory = d.CommentHistory?
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new CommentHistoryDto
                    {
                        CreatedAt = c.CreatedAt,
                        Comment = c.Comment,
                        AuthorFullName = c.Author != null ? c.Author.FullName : "Неизвестный автор",
                        DeadlineId = (int)c.DeadlineId,
                        Id = c.Id // Если нужен ID записи истории
                    })
                    .ToList() ?? new List<CommentHistoryDto>() // защита от null
            }).ToList();
        }

        public async Task<IEnumerable<ReportDto>> GetFilteredReportsAsync(
            string? name,
            int? templateId,
            int? branchId,
            int? year, // Новый параметр
            int? month, // Новый параметр
            int? quarter, // Новый параметр
            int? halfYearPeriod, // Новый параметр
            ReportType? reportType)
        {
            // Включаем Template, т.к. нужен DeadlineType
            var query = _unitOfWork.Reports.GetAll(includes: r => r.Include(d => d.Branch).Include(d => d.Template).Include(u=>u.UploadedBy));

            // Фильтруем только закрытые отчеты для архива
            query = query.Where(rp => rp.IsClosed);

            if (!string.IsNullOrEmpty(name))
                query = query.Where(r => r.Name.Contains(name));

            // Сначала применяем фильтр по шаблону, если он есть
            if (templateId.HasValue)
            {
                query = query.Where(r => r.TemplateId == templateId.Value);

                // *** Логика фильтрации по периоду на основе шаблона и переданных параметров ***
                // Находим DeadlineType выбранного шаблона.
                // Т.к. Template уже включен в query, мы можем потенциально получить его так:
                var selectedTemplate = await _unitOfWork.ReportTemplates.FindAsync(t=>t.Id==templateId.Value);

                DateTime? calculatedStartDate = null;
                DateTime? calculatedEndDate = null;

                // Если шаблон найден и указан год
                if (selectedTemplate != null && year.HasValue)
                {
                    calculatedStartDate = new DateTime(year.Value, 1, 1);
                    calculatedEndDate = new DateTime(year.Value, 12, 31);

                    try
                    {
                        switch (selectedTemplate.DeadlineType)
                        {
                            case DeadlineType.Monthly:
                                if (month.HasValue && month.Value >= 1 && month.Value <= 12)
                                {
                                    calculatedStartDate = new DateTime(year.Value, month.Value, 1);
                                    calculatedEndDate = new DateTime(year.Value, month.Value, DateTime.DaysInMonth(year.Value, month.Value));
                                }
                                // Если month невалиден, останутся значения по умолчанию (годовой период)
                                break;

                            case DeadlineType.Quarterly:
                                if (quarter.HasValue && quarter.Value >= 1 && quarter.Value <= 4)
                                {
                                    int startMonth = (quarter.Value - 1) * 3 + 1;
                                    calculatedStartDate = new DateTime(year.Value, startMonth, 1);
                                    int endMonth = startMonth + 2;
                                    calculatedEndDate = new DateTime(year.Value, endMonth, DateTime.DaysInMonth(year.Value, endMonth));
                                }
                                // Если quarter невалиден, останутся значения по умолчанию (годовой период)
                                break;

                            case DeadlineType.HalfYearly:
                                if (halfYearPeriod.HasValue && halfYearPeriod.Value >= 1 && halfYearPeriod.Value <= 2)
                                {
                                    int startMonth = halfYearPeriod.Value == 1 ? 1 : 7;
                                    calculatedStartDate = new DateTime(year.Value, startMonth, 1);
                                    int endMonth = halfYearPeriod.Value == 1 ? 6 : 12;
                                    calculatedEndDate = new DateTime(year.Value, endMonth, DateTime.DaysInMonth(year.Value, endMonth));
                                }
                                // Если halfYearPeriod невалиден, останутся значения по умолчанию (годовой период)
                                break;

                            case DeadlineType.Yearly:
                                // Для Yearly значения уже установлены по умолчанию, ничего не делаем.
                                break;

                            default:
                                // Если есть другие DeadlineType, которые также должны иметь годовой период по умолчанию
                                // или требуют другой логики, добавьте их здесь.
                                break;
                        }
                    }
                    catch (ArgumentOutOfRangeException)
                    {
                        // Обработка некорректных значений года, месяца и т.д.
                        // В реальном приложении, возможно, стоит логировать или возвращать ошибку.
                        calculatedStartDate = null;
                        calculatedEndDate = null;
                    }
                }

                // Применяем рассчитанный диапазон дат, если он валиден
                if (calculatedStartDate.HasValue && calculatedEndDate.HasValue)
                {
                    // Фильтруем, что Period отчета находится в рассчитанном диапазоне
                    query = query.Where(r => r.Period >= calculatedStartDate.Value && r.Period <= calculatedEndDate.Value);
                }
                // *** Конец логики фильтрации по периоду ***

            }
            // Если templateId не выбран, старые поля StartDate и EndDate не используются,
            // и фильтрация по дате периода не происходит на основе выбора периода,
            // только на основе имени, типа отчета и филиала.

            if (branchId.HasValue)
                query = query.Where(r => r.BranchId == branchId.Value);

            if (reportType.HasValue)
                query = query.Where(r => r.Type == reportType.Value);

            var reports = await query.ToListAsync();

            // Маппинг в DTO остаётся прежним, так как ReportDto уже включает DeadlineType из шаблона
            return reports.Select(r => new ReportDto
            {
                Id = r.Id,
                Name = r.Name,
                SubmissionDate = r.UploadDate,
                UploadedById = r.UploadedById ?? 0,
                BranchId = r.BranchId,
                TemplateId = r.TemplateId,
                FilePath = r.FilePath,
                Period = r.Period,
                Type = r.Type,
                UploadDate = r.UploadDate,
                UploadedByName = r.UploadedBy?.FullName, // Добавляем имя пользователя, если нужно
                // Используем DeadlineType из связанного шаблона
                DeadlineType = r.Template?.DeadlineType ?? 0
            }).ToList();
        }

        public async Task<ReportDto> ReopenReportAsync(int reportid)
        {
            var report = await _unitOfWork.Reports.FindAsync(r => r.Id == reportid);
            var deadline = await _unitOfWork.SubmissionDeadlines.FindAsync(d => d.ReportId == reportid);
            if (report == null|| deadline==null) return null;
            report.IsClosed = false;
            deadline.IsClosed = false;
            deadline.Status = ReportStatus.NeedsCorrection;
            deadline.Reopened = true;
            await _unitOfWork.Reports.UpdateAsync(report);
            await _unitOfWork.SubmissionDeadlines.UpdateAsync(deadline);
            await _unitOfWork.SaveChangesAsync();
            return MapToDto(report);
        }

        private ReportDto MapToDto(Report report)
        {
            return new ReportDto
            {
                Id = report.Id,
                Name = report.Name,
                SubmissionDate = report.UploadDate,
                FilePath = report.FilePath,
                UploadedById = report.UploadedById ?? 0, // Fix for nullable type
                BranchId = report.BranchId,
                TemplateId = report.TemplateId,
                Period = report.Period,
                UploadDate = report.UploadDate,
                Type =report.Type
            };
        }

        private Report MapToEntity(ReportDto reportDto)
        {
            return new Report
            {
                Id = reportDto.Id,
                Name = reportDto.Name,
                UploadDate = reportDto.SubmissionDate,
                FilePath = reportDto.FilePath,
                UploadedById = reportDto.UploadedById,
                BranchId = reportDto.BranchId ?? 0,
                TemplateId = reportDto.TemplateId ?? 0,
                Period = reportDto.Period,
                Type = reportDto.Type
            };
        }
    }
}
