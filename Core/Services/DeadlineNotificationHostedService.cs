using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Core.Services
{
    public class DeadlineNotificationHostedService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;

        public DeadlineNotificationHostedService(IServiceScopeFactory scopeFactory)
        {
            _scopeFactory = scopeFactory;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                using var scope = _scopeFactory.CreateScope();
                var deadlineService = scope.ServiceProvider.GetRequiredService<IDeadlineService>();
                var reportService = scope.ServiceProvider.GetRequiredService<IReportService>();
                var userService = scope.ServiceProvider.GetRequiredService<IUserService>();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();

                var deadlines = await deadlineService.GetAllAsync();
                var today = DateTime.UtcNow.Date;

                foreach (var deadline in deadlines)
                {
                    // для каждого дедлайна — получаем его филиал
                    var branchId = deadline.BranchId!.Value;
                    var reportTemplate = deadline.Template;
                    var deadlineDate = deadline.DeadlineDate.Date;

                    // если отчёт **не загружен** за этот период
                    var existing = await reportService
                        .FindByTemplateBranchPeriodAsync(deadline.ReportTemplateId, branchId, deadlineDate.Year, deadlineDate.Month);
                    if (existing != null)
                        continue;

                    // считаем дни до/после дедлайна
                    var daysLeft = (deadlineDate - today).Days;
                    string? message = daysLeft switch
                    {
                        int d when d == 10 => $"Через 10 дней наступает срок сдачи отчёта: {reportTemplate.Name}",
                        int d when d == 0 => $"Сегодня последний день сдачи отчёта: {reportTemplate.Name}",
                        int d when d < 0 => $"Срок сдачи отчёта «{reportTemplate.Name}» истёк!",
                        _ => null
                    };
                    if (message == null)
                        continue;

                    // 1) Обычные пользователи филиала
                    var branchUsers = await userService.GetUsersByBranchIdAsync(branchId);
                    foreach (var u in branchUsers)
                    {
                            await notificationService.AddNotificationAsync(u.Id, message);
                    }

                    // 2) PEB — если это Plan‑отчёт
                    if (reportTemplate.Type == Enums.ReportType.Plan)
                    {
                        var pebUsers = await userService.GetUsersByRoleAsync("PEB");
                        foreach (var u in pebUsers)
                            await notificationService.AddNotificationAsync(u.Id, message + $" (Филиал: {deadline.Branch!.Name})");
                    }

                    // 3) OBUnF — если это Accountant‑отчёт
                    if (reportTemplate.Type == Enums.ReportType.Accountant)
                    {
                        var obunfUsers = await userService.GetUsersByRoleAsync("OBUnF");
                        foreach (var u in obunfUsers)
                            await notificationService.AddNotificationAsync(u.Id, message + $" (Филиал: {deadline.Branch!.Name})");
                    }
                }

                // ждем сутки
                await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
            }
        }
    }
}
