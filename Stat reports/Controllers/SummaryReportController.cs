using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Stat_reports.ViewModels;
using Stat_reportsnt.Filters;

[AuthorizeBranchAndUser]
[Authorize(Roles = "Admin,AdminTrest,PEB,OBUnF")]
public class SummaryReportController : Controller
{
    private readonly ISummaryReportService _summaryReportService;
    private readonly IReportTemplateService _reportTemplateService;
    private readonly IBranchService _branchService;
    private readonly IConfiguration _configuration;
    public SummaryReportController(ISummaryReportService summaryReportService,
        IReportTemplateService reportTemplateService,
        IBranchService branchService, IConfiguration configuration)
    {
        _summaryReportService = summaryReportService;
        _reportTemplateService = reportTemplateService;
        _branchService = branchService;
        _configuration = configuration;
    }

    [HttpGet]
    public async Task<IActionResult> Summary()
    {
        var model = new SummaryReportGenerationViewModel
        {
            Templates = (List<Core.Entities.ReportTemplate>)
            await _reportTemplateService.GetAllReportTemplatesAsync(),
            Branches = (List<Core.Entities.Branch>)
            await _branchService.GetAllBranchesAsync()
        };

        return View(model);
    }
    
    [HttpPost]
    public async Task<IActionResult> Summary(SummaryReportGenerationViewModel model)
    {
        model.Templates = (List<Core.Entities.ReportTemplate>)
            await _reportTemplateService.GetAllReportTemplatesAsync();
        model.Branches = (List<Core.Entities.Branch>)
            await _branchService.GetAllBranchesAsync();


        if (!ModelState.IsValid || model.SelectedTemplateId == null || model.Year == null)
            return View(model);

        // Получаем ID специального отчета из конфигурации
        // Второй аргумент (9) — это значение по умолчанию
        var fixedStructureTemplateId = _configuration.GetValue<int>("ReportSettings:FixedStructureReportTemplateId", 9);

        // Получаем нужные отчеты
        var reports = await _summaryReportService.
            GetReportsForSummaryAsync(model.SelectedTemplateId.Value,
            model.Year.Value, model.Month, model.Quarter, model.HalfYearPeriod, model.SelectedBranchIds);

        var templatePath = await _summaryReportService.
            GetTemplateFilePathAsync(model.SelectedTemplateId.Value);

        // Получаем выбранный шаблон один раз для определения имени файла
        var selectedTemplate = model.Templates.
            FirstOrDefault(t => t.Id == model.SelectedTemplateId);

        if (selectedTemplate == null)
        {
            // Добавьте обработку ошибки, если шаблон не найден
            return NotFound();
        }
        // Объявляем переменную здесь, чтобы она была доступна для File()
        byte[] mergedExcel;

        if (model.SelectedTemplateId == fixedStructureTemplateId)
        {
            if (model.IsExtendedReport)
            {
                // !!! ВЫЗЫВАЕМ НОВЫЙ МЕТОД ДЛЯ РАСШИРЕННОГО ОТЧЕТА !!!
                mergedExcel = _summaryReportService.MergeSummaryExcelReport(
                    reports,
                    templatePath,
                    (int)model.Year,
                    (int)model.Month);
            }
            else
            {
                // Вызываем старый метод для отчета с фиксированной структурой
                mergedExcel = _summaryReportService.MergeFixedStructureReportsToExcel(
                    reports,
                    templatePath,
                    (int)model.Year,
                    (int)model.Month);
            }

            // Единая точка возврата File для этого блока
            return File(mergedExcel, $"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Сводный {selectedTemplate.Name}.xlsx");
        }
        else
        {
            // Логика для стандартных отчетов
            mergedExcel = _summaryReportService.
                MergeReportsToExcel(reports, templatePath, model.Year.Value, model.Month, model.Quarter, model.HalfYearPeriod);

            return File(mergedExcel, $"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"Сводный {selectedTemplate.Name}.xlsx");
        }
    }
}