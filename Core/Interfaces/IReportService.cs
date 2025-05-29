using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.DTO;
using Core.Entities;
using Core.Enums;
using Microsoft.AspNetCore.Http;

namespace Core.Interfaces
{
    public interface IReportService
    {
        Task<Report?> FindByTemplateBranchPeriodAsync(int templateId, int branchId, int year, int month);
         Task<IEnumerable<Report>> GetAllReportsAsync();
        Task<ReportDto> GetReportByIdAsync(int id);
        Task<ReportDto> CreateReportAsync(ReportDto reportDto);
        Task<ReportDto> UpdateReportAsync(int id, ReportDto reportDto);
        Task<ReportDto> ReopenReportAsync(int reportid);
        Task<bool> DeleteReportAsync(int id);
        Task<IEnumerable<Report>> GetReportsByBranchAsync(int branchId);
        Task<ReportDto> UploadReportAsync(int templateId, int branchId, int uploadedById, IFormFile file,int deadlinId);
        Task<byte[]> GetReportFileAsync(int reportId);
        Task<bool> UpdateReportStatusAsync(int deadlineId,int reportId, ReportStatus newStatus);
        Task<bool> AddReportCommentAsync(int deadlineId,int reportId, string comment, int? authorId);
        Task<List<PendingTemplateDto>> GetPendingTemplatesAsync(int? branchId);
        Task<IEnumerable<ReportDto>> GetFilteredReportsAsync(
    string? name,
    int? templateId,
    int? branchId,
    int? year, // Новый параметр
    int? month, // Новый параметр
    int? quarter, // Новый параметр
    int? halfYearPeriod, // Новый параметр
    ReportType? reportType);
    }
}