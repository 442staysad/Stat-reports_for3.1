using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Interfaces
{
    public interface IExcelSplitterService
    {
        byte[] ProcessReports(List<string> filePaths, string templatePath, int year, int? month, int? quarter, int? halfYear, string signatureFilePath);
        byte[] ProcessFixedStructureReport(List<string> filePaths, string templatePath, int year, int month, string signatureFilePath, List<int> rowRanges);
        byte[] ProcessSummaryExcelReport(List<string> filePaths, string templatePath, int year, int month, string signatureFilePath, List<int> rowRanges);
    }
}
