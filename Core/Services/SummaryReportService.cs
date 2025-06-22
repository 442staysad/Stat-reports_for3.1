using Core.DTO;
using Microsoft.AspNetCore.Hosting;
using Core.Entities;
using Core.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;

namespace Core.Services
{
    public class SummaryReportService : ISummaryReportService
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IExcelSplitterService _excelSplitter;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public SummaryReportService(IUnitOfWork unitOfWork,
                                    IExcelSplitterService excelSplitter, IWebHostEnvironment webHostEnvironment)
        {
            _unitOfWork = unitOfWork;
            _excelSplitter = excelSplitter;
            _webHostEnvironment = webHostEnvironment;
        }

        public async Task<List<Report>> GetReportsForSummaryAsync(int templateId, int year, int? month, int? quarter, int? halfYear, List<int> branchIds)
        {
            var reports = await _unitOfWork.Reports.FindAllAsync(r =>
                r.TemplateId == templateId &&
                r.Period.Year == year &&
                branchIds.Contains((int)r.BranchId));

            if (month != null)
                reports = reports.Where(r => r.Period.Month == month).ToList();
            else if (quarter != null)
            {
                var months = GetQuarterMonths(quarter.Value);
                reports = reports.Where(r => months.Contains(r.Period.Month)).ToList();
            }
            else if (halfYear != null)
            {
                var months = halfYear == 1 ? new[] { 1, 2, 3, 4, 5, 6 } : new[] { 7, 8, 9, 10, 11, 12 };
                reports = reports.Where(r => months.Contains(r.Period.Month)).ToList();
            }

            return reports.ToList();
        }

        public Task<string> GetTemplateFilePathAsync(int templateId)
        {
            // Пусть путь хранится в шаблоне
            return _unitOfWork.ReportTemplates.FindAsync(t => t.Id == templateId).ContinueWith(t => t.Result.FilePath);
        }

        public byte[] MergeReportsToExcel(List<Report> reports, string templatePath, int year, int? month, int? quarter, int? halfYear)
        {
            var paths = reports.Select(r => r.FilePath).ToList();

            // --- ИЗМЕНЕНИЕ ---
            // 1. Получаем путь к папке wwwroot
            string wwwRootPath = _webHostEnvironment.WebRootPath;

            // 2. Формируем полный путь к файлу с подписью
            // Path.Combine - лучший способ для создания кроссплатформенных путей
            string signatureFilePath = Path.Combine(wwwRootPath, "docs", "Подпись.xlsx");

            // 3. Передаем динамический путь в ваш сервис
            byte[] result = _excelSplitter.ProcessReports(
                paths,
                templatePath,
                year,
                month,
                quarter,
                halfYear,
                signatureFilePath // <-- Используем созданный путь
            );

            return result;
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