namespace Stat_reports.ViewModels
{
    public class ReportPreviewViewModel
    {
        public int ReportId { get; set; }
        public string? ReportName { get; set; }
        public byte[] FileContent { get; set; }
    }
}
