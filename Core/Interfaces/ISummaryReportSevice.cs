using Core.DTO;
using Core.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface ISummaryReportService
    {
        Task<List<Report>> GetReportsForSummaryAsync(int templateId, int year, int? month, int? quarter, int? halfYear, List<int> branchIds);
        Task<string> GetTemplateFilePathAsync(int templateId);
        byte[] MergeReportsToExcel(List<Report> reports, string templatePath, int year, int? month, int? quarter, int? halfYear);
        byte[] MergeFixedStructureReportsToExcel(List<Report> reports, string templatePath, int year, int month);
        byte[] MergeSummaryExcelReport(List<Report> reports, string templatePath, int year, int month);
    }
}