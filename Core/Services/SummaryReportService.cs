using Core.DTO;
using Microsoft.AspNetCore.Hosting;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using Core.Enums;

namespace Core.Services
{
    public class SummaryReportService : ISummaryReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelSplitterService _excelSplitter;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SummaryReportService(IUnitOfWork unitOfWork,
                                    IExcelSplitterService excelSplitter,
                                    IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _excelSplitter = excelSplitter;
            _webHostEnvironment = webHostEnvironment;
        }
        public async Task<List<Report>> GetReportsForSummaryAsync(
    int templateId,
    int year,
    int? month,
    int? quarter,
    int? halfYear,
    List<int> branchIds)
        {
            var template = await _unitOfWork.ReportTemplates.FindAsync(t => t.Id == templateId);
            if (template == null)
                return new List<Report>();

            DateTime? startDate = null;
            DateTime? endExclusiveDate = null; // Будет хранить дату начала СЛЕДУЮЩЕГО периода (эксклюзивно)

            if (year > 0)
            {
                startDate = new DateTime(year, 1, 1);
                endExclusiveDate = new DateTime(year + 1, 1, 1); // По умолчанию - начало следующего года

                switch (template.DeadlineType)
                {
                    case DeadlineType.Monthly:
                        if (month.HasValue)
                        {
                            startDate = new DateTime(year, month.Value, 1);

                            // Расчет начала следующего месяца
                            endExclusiveDate = startDate.Value.AddMonths(1);
                        }
                        break;

                    case DeadlineType.Quarterly:
                        if (quarter.HasValue)
                        {
                            int startMonth = (quarter.Value - 1) * 3 + 1;
                            startDate = new DateTime(year, startMonth, 1);

                            // Расчет начала следующего квартала
                            endExclusiveDate = startDate.Value.AddMonths(3);
                        }
                        break;

                    case DeadlineType.HalfYearly:
                        if (halfYear.HasValue)
                        {
                            int startMonth = halfYear.Value == 1 ? 1 : 7;
                            startDate = new DateTime(year, startMonth, 1);

                            // Расчет начала следующего полугодия
                            endExclusiveDate = startDate.Value.AddMonths(6);
                        }
                        break;

                    case DeadlineType.Yearly:
                        // startDate = new DateTime(year, 1, 1); (Уже установлено)
                        // endExclusiveDate = new DateTime(year + 1, 1, 1); (Уже установлено)
                        break;
                }
            }

            // Если даты не установлены, или не удалось их вычислить, возвращаем пустой список
            if (!startDate.HasValue || !endExclusiveDate.HasValue)
            {
                return new List<Report>();
            }

            var query = _unitOfWork.Reports.GetAll().AsQueryable()
                .Where(r =>
                    r.TemplateId == templateId &&
                    r.Period >= startDate.Value &&      // Период >= начало выбранного периода
                    r.Period < endExclusiveDate.Value && // Период < начала СЛЕДУЮЩЕГО периода (РЕШЕНИЕ ПРОБЛЕМЫ)
                    r.BranchId.HasValue &&
                    branchIds.Contains(r.BranchId.Value));

            return await query.ToListAsync();
        }
        /*
                public async Task<List<Report>> GetReportsForSummaryAsync(
                    int templateId,
                    int year,
                    int? month,
                    int? quarter,
                    int? halfYear,
                    List<int> branchIds)
                {
                    var template = await _unitOfWork.ReportTemplates.FindAsync(t => t.Id == templateId);
                    if (template == null)
                        return new List<Report>();

                    DateTime? startDate = null;
                    DateTime? endDate = null;

                    if (year > 0)
                    {
                        startDate = new DateTime(year, 1, 1);
                        endDate = new DateTime(year, 12, 31);

                        switch (template.DeadlineType)
                        {
                            case DeadlineType.Monthly:
                                if (month.HasValue)
                                {
                                    startDate = new DateTime(year, month.Value, 1);
                                    endDate = new DateTime(year, month.Value, DateTime.DaysInMonth(year, month.Value));
                                }
                                break;

                            case DeadlineType.Quarterly:
                                if (quarter.HasValue)
                                {
                                    int startMonth = (quarter.Value - 1) * 3 + 1;
                                    int endMonth = startMonth + 2;
                                    startDate = new DateTime(year, startMonth, 1);
                                    endDate = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));
                                }
                                break;

                            case DeadlineType.HalfYearly:
                                if (halfYear.HasValue)
                                {
                                    int startMonth = halfYear.Value == 1 ? 1 : 7;
                                    int endMonth = halfYear.Value == 1 ? 6 : 12;
                                    startDate = new DateTime(year, startMonth, 1);
                                    endDate = new DateTime(year, endMonth, DateTime.DaysInMonth(year, endMonth));
                                }
                                break;

                            case DeadlineType.Yearly:
                                break;
                        }
                    }

                    var query = _unitOfWork.Reports.GetAll().AsQueryable()
                        .Where(r =>
                            r.TemplateId == templateId &&
                            r.Period >= startDate &&
                            r.Period <= endDate &&
                            r.BranchId.HasValue &&
                            branchIds.Contains(r.BranchId.Value));

                    return await query.ToListAsync();
                }
                */
        public Task<string> GetTemplateFilePathAsync(int templateId)
        {
            return _unitOfWork.ReportTemplates.FindAsync(t => t.Id == templateId).ContinueWith(t => t.Result.FilePath);
        }

        // Старый метод для стандартных отчетов
        public byte[] MergeReportsToExcel(List<Report> reports, string templatePath, int year, int? month, int? quarter, int? halfYear)
        {
            var paths = reports.Select(r => r.FilePath).ToList();
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string signatureFilePath = Path.Combine(wwwRootPath, "docs", "Подпись.xlsx");

            byte[] result = _excelSplitter.ProcessReports(
                paths,
                templatePath,
                year,
                month,
                quarter,
                halfYear,
                signatureFilePath
            );

            return result;
        }

        // --- НОВЫЙ МЕТОД ---
        // Для отчета с фиксированной структурой (копирование столбцов)
        public byte[] MergeFixedStructureReportsToExcel(List<Report> reports, string templatePath, int year, int month)
        {

            // 1. Собираем пути к файлам отчетов
            var paths = reports.Select(r => r.FilePath).ToList();

            // 2. Путь к файлу подписи
            string wwwRootPath = _webHostEnvironment.WebRootPath;
            string signatureFilePath = Path.Combine(wwwRootPath, "docs", "Подпись.xlsx");

            // 3. Вызываем специальный метод в сплиттере
            // Передаем year и month, так как отчет ежемесячный
            return _excelSplitter.ProcessFixedStructureReport(
                paths,
                templatePath,
                year,
                month,
                signatureFilePath
            );
        }

        private List<int> GetQuarterMonths(int quarter)
        {
            return quarter switch
            {
                1 => new List<int> { 1, 2, 3 },
                2 => new List<int> { 4, 5, 6 },
                3 => new List<int> { 7, 8, 9 },
                4 => new List<int> { 10, 11, 12 },
                _ => throw new ArgumentException("Некорректный квартал")
            };
        }
    }
}