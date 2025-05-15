using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stat_reports.ViewModels;
using Stat_reportsnt.Filters;

[AuthorizeBranchAndUser]
[Authorize(Roles = "Admin,AdminTrest,PEB,OBUnF")]
public class SummaryReportController : Controller
{
    private readonly ISummaryReportService _summaryReportService;
    private readonly IReportTemplateService _reportTemplateService;
    private readonly IBranchService _branchService;

    public SummaryReportController(ISummaryReportService summaryReportService,
        IReportTemplateService reportTemplateService,
        IBranchService branchService)
    {
        _summaryReportService = summaryReportService;
        _reportTemplateService = reportTemplateService;
        _branchService = branchService;
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

        // Если только выбрали шаблон — показываем период
        if (model.SelectedTemplateId != null && model.Year == null)
        {
            var selectedTemplate = model.Templates.
                FirstOrDefault(t => t.Id == model.SelectedTemplateId);
            model.PeriodType = selectedTemplate?.DeadlineType;

            return View(model); // показать форму с полями периода
        }

        if (!ModelState.IsValid || model.SelectedTemplateId == null || model.Year == null)
            return View(model);

        // Получаем нужные отчеты
        var reports = await _summaryReportService.
            GetReportsForSummaryAsync(model.SelectedTemplateId.Value,
            model.Year.Value, model.Month, model.Quarter, model.HalfYearPeriod, model.SelectedBranchIds);

        var templatePath = await _summaryReportService.
            GetTemplateFilePathAsync(model.SelectedTemplateId.Value);
        var mergedExcel = _summaryReportService.
          MergeReportsToExcel(reports, templatePath, model.Year.Value, model.Month, model.Quarter, model.HalfYearPeriod);

        return File(mergedExcel, $"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            $"Сводный {model.Templates.FirstOrDefault(t=>t.Id==model.SelectedTemplateId).Name}.xlsx");
    }
}