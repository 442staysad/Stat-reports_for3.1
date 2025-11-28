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
using DocumentFormat.OpenXml.Bibliography;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
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

        public IActionResult UploadReport()
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


            int? userId = HttpContext.Session.GetInt32("UserId");
            int? branchId = HttpContext.Session.GetInt32("BranchId");

            if (userId == null || branchId == null)
            {
                TempData["Error"] = "Ошибка авторизации";
                return RedirectToAction(nameof(WorkingReports));
            }

            await _reportService.UploadReportAsync(templateId, (int)branchId, (int)userId, file,(int)deadlineId);
            TempData["Success"] = "Отчет успешно загружен";

            return RedirectToAction(nameof(WorkingReports));
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Важно!
        public async Task<IActionResult> CancelUpload(int deadlineId)
        {
            bool result = await _deadlineService.CancelUpload(deadlineId);

            if (result) // Предполагаем, что true означает успех
            {
                TempData["Success"] = "Отчет успешно удален.";
            }
            else
            {
                TempData["Error"] = "Ошибка при удалении отчета."; // Обработка ошибки в сервисе
            }
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

            bool isGlobalUser = User.IsInRole("Admin") || User.IsInRole("PEB") || User.IsInRole("OBUnF") || User.IsInRole("AdminTrest");

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
                CommentId = t.CommentId, // Добавляем идентификатор комментария
                LatestCommentAuthorId = t.CommentAuthorId, // Получаем ID последнего автора комментария
                ReportId = t.ReportId,
                ReportType = t.ReportType,
                Period = t.Period, // Добавляем период отчета
                Type = t.Type, // Добавляем тип дедлайна
                BranchId = (int)t.BranchId,
                ReportTypeName = t.ReportType == "Plan" ? "PEB" : "OBUnF",
                BranchName = branches.FirstOrDefault(b => b.Id == t.BranchId)?.Shortname?? "Неизвестный филиал"
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
                return StatusCode(500, "Internal server error");
            }
        }


        // --- Метод для удаления дедлайна ---
        // Этот метод обрабатывает POST-запрос от формы в таблице.
        // Проверка прав производится на сервере перед удалением.
        [HttpPost]
        [Authorize(Roles = "Admin,AdminTrest")]
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
                return RedirectToAction(nameof(WorkingReports));
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
            return RedirectToAction(nameof(ReportArchive));
        }

        [HttpGet("download/{reportId}")]
        public async Task<IActionResult> DownloadReport(int reportId, string reportname)
        {
            var fileBytes = await _reportService.GetReportFileAsync(reportId);
            var report=await _reportService.GetReportByIdAsync(reportId);
            var branch = await _branchService.GetBranchByIdAsync(report.BranchId);
            if (fileBytes == null)
                return NotFound();

            return File(fileBytes, "application/octet-stream", $"{branch.Shortname}_{reportname}.xlsx");
        }

        public async Task<IActionResult> ReportArchive(string? name, int? templateId,
                                                     int? branchId,
                                                     int? year, int? month, int? quarter, int? halfYearPeriod, // Новые параметры
                                                     ReportType? reportType)
        {
            int? sessionBranchId = HttpContext.Session.GetInt32("BranchId");

            if (User.IsInRole("User")||User.IsInRole("AdminBranch"))
            {
                // Если пользователь с ролью User, принудительно устанавливаем его филиал
                branchId = sessionBranchId;
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

        /*--------------------------------------------------------------------------------------------------*/


        // GET: ReportMvc/EditTemplate/5
        [HttpGet]
        [Authorize(Roles = "Admin,PEB,OBUnF,AdminTrest")] // Доступ только тем, кто может управлять шаблонами
        public async Task<IActionResult> EditTemplate(int id)
        {
            var template = await _reportTemplateService.GetReportTemplateByIdAsync(id);
            if (template == null)
            {
                return NotFound($"Шаблон с ID {id} не найден.");
            }

            var model = new EditTemplateViewModel
            {
                Id = template.Id,
                Name = template.Name,
                Description = template.Description,
                Type = template.Type,
                DeadlineType = template.DeadlineType,
                CurrentFilePath = template.FilePath,
                // Получаем разрешенные типы отчетов для текущего пользователя
                AllowedTypes = GetAllowedReportTypes(),
            };

            return View(model);
        }

        // POST: ReportMvc/EditTemplate/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,PEB,OBUnF,AdminTrest")] // Доступ только тем, кто может управлять шаблонами
        public async Task<IActionResult> EditTemplate(EditTemplateViewModel model)
        {
            // Перезаполняем AllowedTypes на случай ошибки валидации
            model.AllowedTypes = GetAllowedReportTypes();

            // Убираем валидацию для NewFile, если оно не загружено (т.к. оно опционально при редактировании)
            if (model.NewFile == null)
            {
                ModelState.Remove(nameof(model.NewFile));
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var templateToUpdate = await _reportTemplateService.GetReportTemplateByIdAsync(model.Id);
                if (templateToUpdate == null)
                {
                    return NotFound();
                }

                // Обновляем свойства
                templateToUpdate.Name = model.Name;
                templateToUpdate.Description = model.Description;
                templateToUpdate.Type = model.Type;
                templateToUpdate.DeadlineType = model.DeadlineType;

                // Обработка файла (только если загружен новый)
                if (model.NewFile != null && model.NewFile.Length > 0)
                {
                    // TODO: Опционально - удалить старый файл _fileService.DeleteFile(templateToUpdate.FilePath);
                    templateToUpdate.FilePath = await _fileService.SaveFileAsync(model.NewFile,"Templates");
                }

                // Вызываем метод сервиса для обновления
                await _reportTemplateService.UpdateReportTemplateAsync(templateToUpdate); // Передаем entity, файл обработали тут

                TempData["SuccessMessage"] = "Шаблон успешно обновлен.";
                return RedirectToAction(nameof(WorkingReports)); // Перенаправляем на список отчетов в работе или другой список
            }
            catch (Exception ex)
            {
                // TODO: Залогировать ошибку
                ModelState.AddModelError("", $"Произошла ошибка при обновлении шаблона: {ex.Message}");
                return View(model);
            }
        }



        // GET: ReportMvc/EditDeadline/5
        [HttpGet]
        [Authorize(Roles = "Admin,PEB,OBUnF,AdminTrest")] // Доступ только тем, кто может управлять дедлайнами
        public async Task<IActionResult> EditDeadline(int id)
        {
            var deadline = await _deadlineService.GetDeadlineByIdAsync(id); // Убедитесь, что сервис включает Branch и Template
            if (deadline == null)
            {
                return NotFound($"Срок сдачи с ID {id} не найден.");
            }

            var branches = await _branchService.GetAllBranchesDtosAsync();
            var templates = await _reportTemplateService.GetAllReportTemplatesAsync();

            var model = new EditDeadlineViewModel
            {
                Id = deadline.Id,
                BranchId = deadline.BranchId,
                ReportTemplateId = deadline.ReportTemplateId,
                DeadlineDate = deadline.DeadlineDate,
                Period = deadline.Period,
                IsClosed = deadline.IsClosed,
                Branches = new SelectList(branches, "Id", "Name", deadline.BranchId),
                Templates = new SelectList(templates, "Id", "Name", deadline.ReportTemplateId),
                BranchName = deadline.Branch?.Name ?? "Неизвестный филиал",
                TemplateName = deadline.Template?.Name ?? "Неизвестный шаблон"
            };

            return View(model);
        }

        // POST: ReportMvc/EditDeadline/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin,PEB,OBUnF,AdminTrest")] // Доступ только тем, кто может управлять дедлайнами
        public async Task<IActionResult> EditDeadline(EditDeadlineViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Перезаполняем dropdowns при ошибке
                var branches = await _branchService.GetAllBranchesDtosAsync();
                var templates = await _reportTemplateService.GetAllReportTemplatesAsync();
                model.Branches = new SelectList(branches, "Id", "Name", model.BranchId);
                model.Templates = new SelectList(templates, "Id", "Name", model.ReportTemplateId);
                return View(model);
            }

            try
            {
                var deadlineToUpdate = await _deadlineService.GetDeadlineByIdAsync(model.Id);
                if (deadlineToUpdate == null)
                {
                    return NotFound();
                }

                // Получаем шаблон, чтобы обновить DeadlineType, если шаблон изменился
                var template = await _reportTemplateService.GetReportTemplateByIdAsync(model.ReportTemplateId);
                if (template == null)
                {
                    ModelState.AddModelError("ReportTemplateId", "Выбранный шаблон не найден.");
                    // Перезаполняем dropdowns
                    var branches = await _branchService.GetAllBranchesDtosAsync();
                    var templates = await _reportTemplateService.GetAllReportTemplatesAsync();
                    model.Branches = new SelectList(branches, "Id", "Name", model.BranchId);
                    model.Templates = new SelectList(templates, "Id", "Name", model.ReportTemplateId);
                    return View(model);
                }

                // Обновляем свойства
                deadlineToUpdate.BranchId = model.BranchId;
                deadlineToUpdate.ReportTemplateId = model.ReportTemplateId;
                deadlineToUpdate.DeadlineDate = model.DeadlineDate;
                deadlineToUpdate.Period = model.Period;
                deadlineToUpdate.IsClosed = model.IsClosed;
                deadlineToUpdate.DeadlineType = template.DeadlineType; // Обновляем тип из шаблона

                await _deadlineService.UpdateDeadlineAsync(deadlineToUpdate);

                TempData["SuccessMessage"] = "Срок сдачи успешно обновлен.";
                return RedirectToAction(nameof(WorkingReports));
            }
            catch (Exception ex)
            {
                // TODO: Залогировать ошибку
                ModelState.AddModelError("", $"Произошла ошибка при обновлении срока сдачи: {ex.Message}");
                // Перезаполняем dropdowns при ошибке
                var branches = await _branchService.GetAllBranchesDtosAsync();
                var templates = await _reportTemplateService.GetAllReportTemplatesAsync();
                model.Branches = new SelectList(branches, "Id", "Name", model.BranchId);
                model.Templates = new SelectList(templates, "Id", "Name", model.ReportTemplateId);
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBranchesWithAcceptedReports(int templateId, int year, int? month, int? quarter, int? halfYear)
        {
            if (templateId == 0)
            {
                return Json(new List<BranchDto>()); // Возвращаем пустой список, если шаблон не выбран
            }

            try
            {
                var branches = await _branchService.GetBranchesWithAcceptedReportsAsync(templateId, year, month, quarter, halfYear);
                return Json(branches);
            }
            catch (Exception ex)
            {
                // TODO: Залогировать ошибку
                return StatusCode(500, "Произошла ошибка при загрузке филиалов.");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Защита от CSRF
        public async Task<IActionResult> UpdateComment([FromBody] UpdateCommentRequest model)
        {
            if (model == null || string.IsNullOrWhiteSpace(model.CommentText))
            {
                return BadRequest(new { success = false, message = "Текст комментария не может быть пустым." });
            }

            try
            {
                // Получаем ID текущего пользователя
                var currentUserId = HttpContext.Session.GetInt32("UserId");

                // Вызываем сервис для обновления комментария
                // Этот метод в сервисе должен проверить, что currentUserId совпадает с автором комментария
                var updatedComment = await _deadlineService.UpdateCommentAsync(model.CommentId, model.CommentText, (int)currentUserId);

                if (updatedComment == null)
                {
                    // Сервис вернул null, значит, у пользователя нет прав или комментарий не найден
                    return Forbid(); // Или NotFound()
                }
                // Возвращаем успешный JSON-ответ
                return Json(new { success = true, newText = updatedComment.Comment });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(); // Пользователь не является автором
            }
            catch (Exception ex)
            {
                // TODO: Залогировать ошибку
                return StatusCode(500, new { success = false, message = "Внутренняя ошибка сервера при обновлении комментария." });
            }
        }

        // Вспомогательный класс для приема данных от AJAX запроса
        public class UpdateCommentRequest
        {
            public int CommentId { get; set; }
            public string CommentText { get; set; }
        }
    }
}
