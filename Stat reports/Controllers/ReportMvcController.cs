using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;
using Core.Enums;
using Core.Interfaces;
using Core.Services;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Stat_reports.ViewModels;
using Stat_reportsnt.Filters;
namespace Stat_reports.Controllers
{
    [AuthorizeBranchAndUser]
    public class ReportMvcController : Controller
    {
        private readonly IReportService _reportService;
        private readonly IDeadlineService _deadlineService;
        private readonly IBranchService _branchService;
        private readonly IReportTemplateService _reportTemplateService;
        private readonly IFileService _fileService;

        public ReportMvcController(IReportService reportService,
            IDeadlineService deadlineService,
            IBranchService branchService,
            IReportTemplateService reportTemplateService,
            IFileService fileService)
        {
            _reportService = reportService;
            _deadlineService = deadlineService;
            _branchService = branchService;
            _reportTemplateService = reportTemplateService;
            _fileService = fileService;
        }

        public async Task<IActionResult> Index()
        {
            if (!HttpContext.Session.TryGetValue("BranchId", out var branchIdBytes))
            {
                return RedirectToAction("BranchLogin", "Auth"); // Перенаправляем на вход филиала
            }

            int branchId = BitConverter.ToInt32(branchIdBytes, 0);
            var reports = await _reportService.GetReportsByBranchAsync(branchId);
            return View(reports);
        }

        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> UploadReport(int templateId, IFormFile file, int? deadlineId = null)
        {
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Файл не выбран";
                return RedirectToAction(nameof(WorkingReports));
            }

            // Проверка расширения
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var allowedExtensions = new[] { ".xls", ".xlsx" };

            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Разрешены только Excel-файлы (.xls, .xlsx)";
                return RedirectToAction(nameof(WorkingReports));
            }

            int? userId = HttpContext.Session.GetInt32("UserId");
            int? branchId = HttpContext.Session.GetInt32("BranchId");

            if (userId == null || branchId == null)
            {
                TempData["Error"] = "Ошибка авторизации";
                return RedirectToAction(nameof(WorkingReports));
            }

            await _reportService.UploadReportAsync(templateId, branchId.Value, userId.Value, file);
            TempData["Success"] = "Отчет успешно загружен";

            return RedirectToAction(nameof(WorkingReports));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var report = await _reportService.GetReportByIdAsync(id);
            if (report == null)
                return NotFound();
            return View(report);
        }


        public async Task<IActionResult> PreviewExcel(int reportId, int? deadlineId, bool isArchive = false)
        {
            var report = await _reportService.GetReportByIdAsync(reportId);
            if (report == null || string.IsNullOrEmpty(report.FilePath))
                return NotFound("Файл отчета не найден");

            var branch = await _branchService.GetBranchByIdAsync(report.BranchId);

            var model = new ExcelPreviewViewModel
            {
                ReportType = report.Type == ReportType.Accountant ? "OBUnF" : "PEB",
                BranchName = branch.Name,
                DeadlineId = deadlineId,
                ReportId = reportId,
                ReportName = report.Name,
                FilePath = report.FilePath,
                Comment = report.Comment,
                Status = report.Status,
                CommentHistory = report.CommentHistory,
                IsArchive = isArchive
            };

            return View(model);
        }
        // в ReportMvcController
        [HttpGet]
        public async Task<IActionResult> DownloadExcelForView(int reportId)
        {
            var report = await _reportService.GetReportByIdAsync(reportId);
            if (report == null || string.IsNullOrEmpty(report.FilePath))
                return NotFound();

            var bytes = await _fileService.GetFileAsync(report.FilePath);
            return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        Path.GetFileName(report.FilePath));
        }

        [HttpPost]
        [Authorize(Roles = "Admin,PEB,OBUnF")]
        public async Task<IActionResult> AddComment(int deadlineId, int reportId, string comment)
        {
            await _reportService.AddReportCommentAsync(deadlineId, reportId, comment, HttpContext.Session.GetInt32("UserId"));
            //await _reportService.UpdateReportStatusAsync(reportId, ReportStatus.NeedsCorrection,comment);
            return RedirectToAction(nameof(WorkingReports));
        }

        [HttpGet]
        [Authorize(Roles = "Admin,PEB,OBUnF")]
        public IActionResult CreateTemplate()
        {
            if (!User.IsInRole("Admin") && !User.IsInRole("PEB") && !User.IsInRole("OBUnF"))
                return Forbid();

            ViewBag.AllowedTypes = GetAllowedReportTypes();
            return View();
        }

        [HttpPost]
        [Authorize(Roles = "Admin,PEB,OBUnF")]
        public async Task<IActionResult> CreateTemplate(CreateTemplateViewModel model)
        {/*
            if (!ModelState.IsValid)
            {
                ViewBag.AllowedTypes = GetAllowedReportTypes();
                return View(model);
            }*/


            string filePath = null;
            if (model.File != null)
            {
                filePath = await _fileService.SaveFileAsync(model.File, "Templates");
            }

            var template = new ReportTemplate
            {
                Name = model.Name,
                Description = model.Description,
                FilePath = filePath, // сюда кладем путь к загруженному файлу
                Type = model.Type,
                DeadlineType = model.DeadlineType
            };

            await _reportTemplateService.CreateReportTemplateAsync(template, model.DeadlineType, model.FixedDay, model.ReportDate);

            return RedirectToAction("WorkingReports");
        }

        private List<ReportType> GetAllowedReportTypes()
        {
            var types = new List<ReportType>();

            if (User.IsInRole("Admin"))
            {
                types.Add(ReportType.Plan);
                types.Add(ReportType.Accountant);
            }
            else if (User.IsInRole("PEB"))
            {
                types.Add(ReportType.Plan);
            }
            else if (User.IsInRole("OBUnF"))
            {
                types.Add(ReportType.Accountant);
            }

            return types;
        }

        [HttpPost]
        [Authorize(Roles = "Admin,PEB,OBUnF")]
        public async Task<IActionResult> AcceptReport(int deadlineId, int reportId)
        {
            await _reportService.UpdateReportStatusAsync(deadlineId, reportId, ReportStatus.Reviewed);
            return RedirectToAction(nameof(WorkingReports));
        }

        public async Task<IActionResult> WorkingReports()
        {
            int? sessionBranchId = HttpContext.Session.GetInt32("BranchId");

            bool isGlobalUser = User.IsInRole("Admin") || User.IsInRole("PEB") || User.IsInRole("OBUnF") || User.IsInRole("Trest");

            // Только если не глобальный пользователь — ограничиваем по филиалу
            int? branchId = isGlobalUser ? null : sessionBranchId;

            var templates = await _reportService.GetPendingTemplatesAsync(branchId);

            var branches = await _branchService.GetAllBranchesAsync();

            var viewModel = templates.Select(t => new PendingTemplateViewModel
            {
                DeadlineId = t.Id,
                TemplateId = t.TemplateId,
                TemplateName = t.TemplateName,
                Deadline = t.Deadline,
                Status = t.Status,
                Comment = t.Comment,
                ReportId = t.ReportId,
                ReportType = t.ReportType,
                BranchId = (int)t.BranchId,
                ReportTypeName = t.ReportType == "Plan" ? "PEB" : "OBUnF",
                BranchName = branches.FirstOrDefault(b => b.Id == t.BranchId)?.Name ?? "Неизвестный филиал"
            }).ToList();

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ReportDto reportDto)
        {
            if (!ModelState.IsValid)
                return View(reportDto);

            await _reportService.UpdateReportAsync(id, reportDto);
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = "Admin,PEB,OBUnF,AdminTrest")]
        public async Task<IActionResult> Delete(int id)
        {
            await _reportService.DeleteReportAsync(id);
            return RedirectToAction("ReportArchive", "ReportMvc");
        }

        [HttpGet]
        [Authorize(Roles = "Admin,AdminTrest,PEB,OBUnF")]
        public async Task<IActionResult> GetTemplatesForManagement()
        {
            Console.WriteLine("GetTemplatesForManagement called");

            try
            {
                var templates = await _reportTemplateService.GetAllReportTemplatesAsync();
                Console.WriteLine($"Found {templates.Count()} templates");

                var templateList = templates.Select(t => new
                {
                    Id = t.Id,
                    Name = t.Name,
                    Type = t.Type.ToString()
                }).ToList();

                return Json(templateList);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message, "Error in GetTemplatesForManagement");
                return StatusCode(500, "Internal server error");
            }
        }


        // --- Метод для удаления дедлайна ---
        // Этот метод обрабатывает POST-запрос от формы в таблице.
        // Проверка прав производится на сервере перед удалением.
        [HttpPost]
        [Authorize(Roles = "Admin,AdminTrest,PEB,OBUnF")]
        [ValidateAntiForgeryToken] // Важно!
        public async Task<IActionResult> DeleteDeadline(int id)
        {
            var deadline = await _deadlineService.GetDeadlineByIdAsync(id);

            if (deadline == null)
            {
                TempData["Error"] = "Срок сдачи не найден.";
                return RedirectToAction(nameof(WorkingReports)); // Или NotFound()
            }

            // --- Серверная проверка прав на удаление конкретного дедлайна ---
            bool canDeleteAny = User.IsInRole("Admin") || User.IsInRole("AdminTrest");
            bool canDeletePlan = User.IsInRole("PEB") && deadline.Template?.Type == ReportType.Plan; // Проверяем тип шаблона дедлайна
            bool canDeleteAccountant = User.IsInRole("OBUnF") && deadline.Template?.Type == ReportType.Accountant; // Проверяем тип шаблона дедлайна

            if (!canDeleteAny && !canDeletePlan && !canDeleteAccountant)
            {
                // Если у пользователя нет прав на удаление этого дедлайна
                TempData["Error"] = "У вас нет прав на удаление этого срока сдачи отчета.";
                return RedirectToAction(nameof(WorkingReports)); // Или Forbid()
            }

            // Если права есть, выполняем удаление
            var result = await _deadlineService.DeleteDeadlineAsync(id); // Убедитесь, что сервис возвращает bool или другой индикатор успеха

            if (result) // Предполагаем, что true означает успех
            {
                TempData["Success"] = "Срок сдачи успешно удален.";
            }
            else
            {
                TempData["Error"] = "Ошибка при удалении срока сдачи."; // Обработка ошибки в сервисе
            }

            return RedirectToAction(nameof(WorkingReports)); // Перенаправляем обратно на страницу
        }


        // --- Метод для удаления шаблона ---
        // Этот метод будет обрабатывать AJAX POST-запрос из модального окна.
        // Проверка прав производится на сервере перед удалением.
        // Возвращает JSON статус, а не перенаправление.
        [HttpPost]
        [Authorize(Roles = "Admin,AdminTrest,PEB,OBUnF")]
        [ValidateAntiForgeryToken] // Важно!
        public async Task<IActionResult> DeleteTemplate(int id)
        {
            var template = await _reportTemplateService.GetReportTemplateByIdAsync(id);

            if (template == null)
            {
                // Возвращаем статус 404 Not Found, если шаблон не найден
                return NotFound(new { success = false, message = "Шаблон не найден." });
            }

            // --- Серверная проверка прав на удаление конкретного шаблона ---
            bool canDeleteAny = User.IsInRole("Admin") || User.IsInRole("AdminTrest");
            bool canDeletePlan = User.IsInRole("PEB") && template.Type == ReportType.Plan; // Проверяем тип шаблона
            bool canDeleteAccountant = User.IsInRole("OBUnF") && template.Type == ReportType.Accountant; // Проверяем тип шаблона

            if (!canDeleteAny && !canDeletePlan && !canDeleteAccountant)
            {
                // Если у пользователя нет прав на удаление этого шаблона
                // Возвращаем статус 403 Forbidden
                return Forbid(); // Это вернет 403 Forbidden без тела, или можно вернуть Unauthorized() / BadRequest()
                                 // Для более информативного ответа в AJAX можно вернуть:
                                 // return StatusCode(403, new { success = false, message = "У вас нет прав на удаление этого шаблона." });
            }

            // Если права есть, выполняем удаление
            var result = await _reportTemplateService.DeleteReportTemplateAsync(id); // Убедитесь, что сервис возвращает bool или другой индикатор успеха

            if (result) // Предполагаем, что true означает успех
            {
                // Возвращаем статус 200 OK с индикатором успеха
                return Ok(new { success = true, message = "Шаблон успешно удален." });
            }
            else
            {
                // Если сервис вернул ошибку
                // Возвращаем статус 400 Bad Request с сообщением об ошибке
                return BadRequest(new { success = false, message = "Ошибка при удалении шаблона." });
            }
        }


        [HttpPost]
        [Authorize(Roles = "Admin,PEB,OBUnF,AdminTrest")]
        public async Task<IActionResult> ReopenReport(int reportId)
        {
            await _reportService.ReopenReportAsync(reportId);
            return View();
        }

        [HttpGet("download/{reportId}")]
        public async Task<IActionResult> DownloadReport(int reportId, string reportname)
        {
            var fileBytes = await _reportService.GetReportFileAsync(reportId);

            if (fileBytes == null)
                return NotFound();

            return File(fileBytes, "application/octet-stream", $"{reportname}.xlsx");
        }

        public async Task<IActionResult> ReportArchive(string? name, int? templateId,
                                                     int? branchId,
                                                     int? year, int? month, int? quarter, int? halfYearPeriod, // Новые параметры
                                                     ReportType? reportType)
        {
            int? sessionBranchId = HttpContext.Session.GetInt32("BranchId");

            if (User.IsInRole("User"))
            {
                // Если пользователь с ролью User, принудительно устанавливаем его филиал
                branchId = sessionBranchId;
            }
            else if (!User.IsInRole("Admin") && !User.IsInRole("PEB") && !User.IsInRole("OBUnF"))
            {
                // Если пользователь не админ, PEB или OBUnF, и не User с установленным филиалом,
                // возможно, вы захотите перенаправить его или показать сообщение.
                // Или просто не применять фильтр по филиалу, если он не установлен.
                // В данном случае, если branchId пришел null (например, из сброса фильтров)
                // и пользователь не User, фильтр по филиалу не применяется.
            }


            // Передаем новые параметры в сервис
            var reports = await _reportService.GetFilteredReportsAsync(
                name, templateId, branchId, year, month, quarter, halfYearPeriod, reportType);

            var branches = await _branchService.GetAllBranchesAsync();
            var templates = await _reportTemplateService.GetAllReportTemplatesAsync();

            var model = new ReportArchiveViewModel
            {
                Reports = reports,
                Branches = branches,
                Templates = templates,
                Filter = new ReportFilterViewModel
                {
                    Name = name,
                    TemplateId = templateId,
                    BranchId = branchId,
                    Year = year, // Сохраняем выбранные значения для отображения в форме
                    Month = month,
                    Quarter = quarter,
                    HalfYearPeriod = halfYearPeriod,
                    Type = reportType
                }
            };

            return View(model);
        }
    }
}
